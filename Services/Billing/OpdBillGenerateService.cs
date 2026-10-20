using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models.Legacy;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// BILL GENERATION for one outpatient file - the single shared implementation of
    /// VFP form <c>GENBILL</c> (cmdok.Click).  Used by
    ///   * /opd/Billing                (Officer, single file)
    ///   * /billing/GenerateOpdBill   (batch, many files)
    ///
    /// Business rules (identical to the VFP procedure)
    ///   1. Next bill number = BCONTROL.OPDBILL + 1, allocated under UPDLOCK, ROWLOCK
    ///      inside the transaction (VFP table-buffered +1 was not atomic).
    ///   2. INSERT OPDBILL header (billno, bdate, sdate, edate, hos_code, pcode, cid, ntot).
    ///   3. Stamp every UNBILLED slip of the file IN THE PERIOD: OBIL_MST.BILL = billno, BDATE = bill date.
    ///      NOTE (unlike IPD/GENBILLI): DATE RANGE FILTER - only slips with SLIPDT between
    ///      periodStart and periodEnd are included.
    ///   4. REGISTER.BILLGEN = 1, TBILLAMT = total.
    ///   5. Returns (TYPE='R') are subtracted from the total.
    ///   6. The whole thing is one transaction - it either fully succeeds or fully rolls back.
    ///
    /// Side effects: BCONTROL, OPDBILL, OBIL_MST, REGISTER are updated; no rows are deleted.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 15:40
    /// </summary>
    public class OpdBillGenerateService
    {
        private readonly ApplicationDbContext _db;

        public OpdBillGenerateService(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Generates the bill for a single registration.
        /// </summary>
        /// <param name="regNo">REGISTER.REGNO (TYPE='O').</param>
        /// <param name="billDate">Bill date stamped on the header and the slips.</param>
        /// <param name="periodStart">SDATE (optional) - start of slip date range.</param>
        /// <param name="periodEnd">EDATE (optional) - end of slip date range.</param>
        /// <param name="user">Web user name stamped as USERID.</param>
        /// <returns>The allocated bill number.</returns>
        /// <exception cref="InvalidOperationException">No unbilled slips for the file in the period.</exception>
        public async Task<long> GenerateForRegistrationAsync(string regNo, DateTime billDate,
            DateTime? periodStart, DateTime? periodEnd, string user)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            // Build the WHERE clause for the date range (GENBILL semantics)
            string dateFilter = "";
            if (periodStart.HasValue && periodEnd.HasValue)
            {
                dateFilter = " AND SLIPDT >= @ps AND SLIPDT <= @pe";
            }

            // --- 1. net total of the unbilled slips in the period (returns subtracted) ---
            using (var c1 = conn.CreateCommand())
            {
                c1.CommandText = $@"SELECT ISNULL(SUM(CASE WHEN TYPE <> 'R' THEN NTOTAL ELSE NTOTAL * -1 END), 0)
                                      FROM OBIL_MST WITH (UPDLOCK)
                                     WHERE HOS_CODE = @r AND (BILL IS NULL OR BILL = 0){dateFilter}";
                AddParam(c1, "@r", regNo);
                if (periodStart.HasValue) AddParam(c1, "@ps", periodStart.Value);
                if (periodEnd.HasValue) AddParam(c1, "@pe", periodEnd.Value);
                var n = await c1.ExecuteScalarAsync();
                var total = Convert.ToDecimal(n ?? 0m);
                if (total == 0m)
                    throw new InvalidOperationException("No unbilled slips found for this registration in the selected period.");

                // --- 2. allocate the bill number atomically ---
                long billNo;
                using (var c2 = conn.CreateCommand())
                {
                    c2.CommandText = "SELECT OPDBILL FROM BCONTROL WITH (UPDLOCK, ROWLOCK) WHERE Id = 1;";
                    billNo = Convert.ToInt64(await c2.ExecuteScalarAsync()) + 1;
                }
                using (var c2b = conn.CreateCommand())
                {
                    c2b.CommandText = "UPDATE BCONTROL SET OPDBILL = @n WHERE Id = 1;";
                    AddParam(c2b, "@n", billNo);
                    await c2b.ExecuteNonQueryAsync();
                }

                // --- 3. patient / company of the file ---
                string pcode = null, cid = null;
                using (var c3 = conn.CreateCommand())
                {
                    c3.CommandText = "SELECT TOP 1 PCODE, CID FROM REGISTER WHERE TYPE = 'O' AND REGNO = @r;";
                    AddParam(c3, "@r", regNo);
                    using (var rd = await c3.ExecuteReaderAsync())
                        if (await rd.ReadAsync()) { pcode = rd.IsDBNull(0) ? null : rd.GetString(0); cid = rd.IsDBNull(1) ? null : rd.GetString(1); }
                }

                // --- 4. insert the header ---
                using (var c4 = conn.CreateCommand())
                {
                    c4.CommandText = @"INSERT INTO OPDBILL
                        (BILLNO, BDATE, SDATE, EDATE, HOS_CODE, PCODE, CID, NTOT, RDATE, STAT, USERID, UDATE, UTIME)
                       VALUES
                        (@bn, @bd, @sd, @ed, @r, @pc, @cid, @tot, @rd, '', @u,
                         CONVERT(varchar(10), @bd, 120), CONVERT(varchar(8), @bd, 108))";
                    AddParam(c4, "@bn", billNo);
                    AddParam(c4, "@bd", billDate);
                    AddParam(c4, "@sd", periodStart ?? (object)DBNull.Value);
                    AddParam(c4, "@ed", periodEnd ?? (object)DBNull.Value);
                    AddParam(c4, "@r", regNo);
                    AddParam(c4, "@pc", (object)pcode ?? DBNull.Value);
                    AddParam(c4, "@cid", (object)cid ?? DBNull.Value);
                    AddParam(c4, "@tot", total);
                    AddParam(c4, "@rd", DateTime.Today);
                    AddParam(c4, "@u", user ?? (object)DBNull.Value);
                    await c4.ExecuteNonQueryAsync();
                }

                // --- 5. stamp every unbilled slip IN THE PERIOD (GENBILL semantics) ---
                using (var c5 = conn.CreateCommand())
                {
                    c5.CommandText = $@"UPDATE OBIL_MST SET BILL = @bn, BDATE = @bd
                                          WHERE HOS_CODE = @r AND (BILL IS NULL OR BILL = 0){dateFilter}";
                    AddParam(c5, "@bn", billNo);
                    AddParam(c5, "@bd", billDate);
                    AddParam(c5, "@r", regNo);
                    if (periodStart.HasValue) AddParam(c5, "@ps", periodStart.Value);
                    if (periodEnd.HasValue) AddParam(c5, "@pe", periodEnd.Value);
                    await c5.ExecuteNonQueryAsync();
                }

                // --- 6. register counters ---
                using (var c6 = conn.CreateCommand())
                {
                    c6.CommandText = @"UPDATE REGISTER SET BILLGEN = 1, TBILLAMT = @t
                                        WHERE TYPE = 'O' AND REGNO = @r";
                    AddParam(c6, "@t", total);
                    AddParam(c6, "@r", regNo);
                    await c6.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return billNo;
            }
        }

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}