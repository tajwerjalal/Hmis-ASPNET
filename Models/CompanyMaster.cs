using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// The legacy VFP company master - [HMIS].[dbo].[COMPANY].
    ///
    /// The table carries no primary key and no discriminator, so it is mapped
    /// as a KEYLESS query type: reads go through EF normally, while inserts
    /// and updates are executed as parameterised SQL (the master is written
    /// with raw statements in the Corporate Companies page).
    ///
    /// The corporate companies screen lists this table (it holds the real
    /// company register - 170 rows, codes A001 ...) and joins it to
    /// [INSTITUTION] on Code = ID for the registration number, the e-mail and
    /// the company service rates, which the billing engine keeps under the
    /// INSTITUTION id.
    ///
    /// Legacy columns: ID (code), NAME, ADDRESS, CITY, ATTN (attention),
    /// DESG (attention designation), PHONE, BILLMODE, STATUS ('A' active /
    /// 'I' inactive), USERID + UDATE + UTIME (last-edit stamp).
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 21:20
    /// Change Log: 22-Oct-2026 21:20 - created so /corporate/companies lists
    ///             the legacy company master instead of the (empty) typed
    ///             INSTITUTION rows.
    /// </summary>
    [Table("COMPANY")]
    public class CompanyMaster
    {
        /// <summary>Company code (legacy ID, varchar 5, e.g. A001).</summary>
        [Column("ID")]
        public string Id { get; set; }

        /// <summary>Company name.</summary>
        [Column("NAME")]
        public string Name { get; set; }

        [Column("ADDRESS")]
        public string Address { get; set; }

        [Column("CITY")]
        public string City { get; set; }

        /// <summary>Contact person (legacy ATTN).</summary>
        [Column("ATTN")]
        public string Attention { get; set; }

        /// <summary>Contact designation (legacy DESG).</summary>
        [Column("DESG")]
        public string AttentionDesignation { get; set; }

        [Column("PHONE")]
        public string Phone { get; set; }

        /// <summary>Billing mode (legacy BILLMODE).</summary>
        [Column("BILLMODE")]
        public string BillMode { get; set; }

        /// <summary>'A' active / 'I' inactive (blank = unknown, treated as active).</summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>Last-edit user (legacy USERID stamp).</summary>
        [Column("USERID")]
        public string UserId { get; set; }

        /// <summary>Last-edit date (legacy UDATE stamp).</summary>
        [Column("UDATE")]
        public DateTime? UpdateDate { get; set; }

        /// <summary>Last-edit time 'HH:mm' (legacy UTIME stamp).</summary>
        [Column("UTIME")]
        public string UpdateTime { get; set; }

        /// <summary>True when the legacy STATUS marks the company inactive.</summary>
        public bool IsInactive => string.Equals(Status, "I", StringComparison.OrdinalIgnoreCase);
    }
}
