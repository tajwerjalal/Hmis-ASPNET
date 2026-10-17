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
    /// BILL GENERATION for one inpatient file - the single shared implementation of
    /// VFP form <c>GENBILLI</c> (cmdok.Click).  Used by
    ///   * /inward/Billing            (Officer, single file)
    ///   * /billing/GenerateIpdBill   (batch, many files)
    ///
    /// Business rules (identical to the VFP procedure)
    ///   1. Next bill number = BCONTROL.IPDBILL + 1, allocated under UPDLOCK, ROWLOCK
    ///      inside the transaction (VFP table-buffered +1 was not atomic).
    ///   2. INSERT IPDBILL header (billno, bdate, sdate, edate, hos_code, pcode, cid, ntot).
    ///   3. Stamp every UNBILLED slip of the file: IBIL_MST.BILL = billno, BDATE = bill date.
    ///      NOTE (unlike OPD/GENBILL): NO date range filter - only "BILL = 0".
    ///   4. REGISTER.BILLGEN = 1, TBILLAMT = total.
    ///   5. Returns (TYPE='R') are subtracted from the total.
    ///   6. The whole thing is one transaction - it either fully succeeds or fully rolls back.
    ///
    /// Side effects: BCONTROL, IPDBILL, IBIL_MST, REGISTER are updated; no rows are deleted.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:10
    /// </summary>
    public class IpdBillGenerateService
    {
        private readonly ApplicationDbContext _db;

        public IpdBillGenerateService(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Generates the bill for a single registration.
        /// </summary>
        /// <param name="regNo">REGISTER.REGNO (TYPE='I').</param>
        /// <param name="billDate">Bill date stamped on the header and the slips.</param>
        /// <param name="periodStart">SDATE (optional).</param>
        /// <param name="periodEnd">EDATE (optional).</param>
        /// <param name="user">Web user name stamped as USERID.</param>
        /// <returns>The allocated bill number.</returns>
        /// <exception cref="InvalidOperationException">No unbilled slips for the file.</exception>
        public async Task<long> GenerateForRegistrationAsync(string regNo, DateTime billDate,
            DateTime? periodStart, DateTime? periodEnd, string user)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            // --- 1. net total of the unbilled slips (returns subtracted) ---
            using (var c1 = conn.CreateCommand())
            {
                c1.CommandText = @"SELECT ISNULL(SUM(CASE WHEN TYPE <> 'R' THEN NTOTAL ELSE NTOTAL * -1 END), 0)
                                     FROM IBIL_MST WITH (UPDLOCK)
                                    WHERE HOS_CODE = @r AND (BILL IS NULL OR BILL = 0)";
                AddParam(c1, "@r", regNo);
                var n = await c1.ExecuteScalarAsync();
                var total = Convert.ToDecimal(n ?? 0m);
                if (total == 0m)
                    throw new InvalidOperationException("No unbilled slips found for this registration.");

                // --- 2. allocate the bill number atomically ---
                long billNo;
                using (var c2 = conn.CreateCommand())
                {
                    c2.CommandText = "SELECT IPDBILL FROM BCONTROL WITH (UPDLOCK, ROWLOCK) WHERE Id = 1;";
                    billNo = Convert.ToInt64(await c2.ExecuteScalarAsync()) + 1;
                }
                using (var c2b = conn.CreateCommand())
                {
                    c2b.CommandText = "UPDATE BCONTROL SET IPDBILL = @n WHERE Id = 1;";
                    AddParam(c2b, "@n", billNo);
                    await c2b.ExecuteNonQueryAsync();
                }

                // --- 3. patient / company of the file ---
                string pcode = null, cid = null;
                using (var c3 = conn.CreateCommand())
                {
                    c3.CommandText = "SELECT TOP 1 PCODE, CID FROM REGISTER WHERE TYPE = 'I' AND REGNO = @r;";
                    AddParam(c3, "@r", regNo);
                    using (var rd = await c3.ExecuteReaderAsync())
                        if (await rd.ReadAsync()) { pcode = rd.IsDBNull(0) ? null : rd.GetString(0); cid = rd.IsDBNull(1) ? null : rd.GetString(1); }
                }

                // --- 4. insert the header ---
                using (var c4 = conn.CreateCommand())
                {
                    c4.CommandText = @"INSERT INTO IPDBILL
                        (BILLNO, BDATE, SDATE, EDATE, HOS_CODE, PCODE, CID, NTOT, RDATE, STAT, USERID, UDATE, UTIME)
                       VALUES
                        (@bn, @bd, @sd, @ed, @r, @pc, @cid, @tot, @rd, 'I', @u,
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

                // --- 5. stamp every unbilled slip (NO date filter - VFP GENBILLI) ---
                using (var c5 = conn.CreateCommand())
                {
                    c5.CommandText = @"UPDATE IBIL_MST SET BILL = @bn, BDATE = @bd
                                         WHERE HOS_CODE = @r AND (BILL IS NULL OR BILL = 0)";
                    AddParam(c5, "@bn", billNo);
                    AddParam(c5, "@bd", billDate);
                    AddParam(c5, "@r", regNo);
                    await c5.ExecuteNonQueryAsync();
                }

                // --- 6. register counters ---
                using (var c6 = conn.CreateCommand())
                {
                    c6.CommandText = @"UPDATE REGISTER SET BILLGEN = 1, TBILLAMT = @t
                                        WHERE TYPE = 'I' AND REGNO = @r";
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
