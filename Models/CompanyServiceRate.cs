using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Company-specific service rate (panel / corporate agreements) - the
    /// single company-charge table used by billing, the corporate rate card
    /// and every department charge screen.
    ///
    /// Migrated from the VFP master compchg.DBF on 22-Oct-2026 and rebuilt on
    /// 22-Oct-2026 with the REAL widths of that file instead of the previous
    /// blanket nvarchar(max)/float:
    ///
    ///   compchg field   DBF type     SQL column          SQL type
    ///   CID             C(4)         CompanyCode         varchar(4)
    ///   SCODE           C(5)         ServiceCode         varchar(5)
    ///   SRATE           N(8,2)       Rate                decimal(8,2)
    ///   URATE           N(8,2)       URate               decimal(8,2)
    ///   DEPT            C(2)         DEPT                varchar(2)
    ///   CHANGE          L            ChangeFlag          varchar(1) 'Y'/'N'
    ///   USERID          C(10)        UserId              varchar(10)
    ///   UDATE           D            UDate               date
    ///   UTIME           C(5)         UTime               varchar(5)
    ///
    /// App columns keep their billing meaning but are sized the same way:
    ///   CompanyId / ItemId   -> resolved ids (nullable: 214 legacy services
    ///                           were never migrated)
    ///   StaffRate / UnitRate -> decimal(8,2)
    ///   DiscountPercent      -> decimal(5,2)
    ///   EffectiveFrom/To     -> date
    ///   RetireComments       -> varchar(500)
    ///
    /// Money is decimal, not float: this table drives billing and float
    /// rounding is not acceptable for charges. Codes stay varchar to match
    /// [COMPANY].ID and [SERVICES].CODE (both varchar) and avoid the implicit
    /// nvarchar conversions that would turn every lookup into a scan.
    ///
    /// Developer : [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 - reloaded from compchg.DBF (legacy codes).
    ///             22-Oct-2026 - realistic types: money -> decimal(8,2),
    ///             codes -> exact DBF widths (varchar), dates -> date,
    ///             duplicate legacy keys collapsed to one live row each
    ///             (21,169 rows), filtered unique index on the live key.
    /// </summary>
    [Table("COMPANYSERVICERATE")]
    public class CompanyServiceRate
    {
        [Key]
        public long Id { get; set; }

        /// <summary>Company (panel) - resolved from the legacy company code.</summary>
        public long? CompanyId { get; set; }

        /// <summary>Billing item - resolved from the legacy service code.</summary>
        public long? ItemId { get; set; }

        /// <summary>Standard company rate (legacy SRATE, N(8,2)).</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal Rate { get; set; }

        /// <summary>Foreigner rate (legacy URATE, N(8,2)).</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal? URate { get; set; }

        /// <summary>Staff rate (app column, same money precision as Rate).</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal? StaffRate { get; set; }

        /// <summary>Unit / urgent rate (app column).</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal? UnitRate { get; set; }

        /// <summary>Organisation unit that loaded the rate (not the service dept).</summary>
        public long? DepartmentId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercent { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EffectiveFrom { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EffectiveTo { get; set; }

        public bool Retired { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string RetireComments { get; set; }

        public long? CreaterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public long? EditerId { get; set; }

        public DateTime? EditedAt { get; set; }

        // ---- legacy master columns (compchg.DBF) - the widths are the DBF widths ----

        /// <summary>Legacy company code (CID, C(4)).</summary>
        [MaxLength(4)]
        [Column(TypeName = "varchar(4)")]
        public string CompanyCode { get; set; }

        /// <summary>Legacy service code (SCODE, C(5)) - matches SERVICES.CODE.</summary>
        [MaxLength(5)]
        [Column(TypeName = "varchar(5)")]
        public string ServiceCode { get; set; }

        /// <summary>Legacy service department code (DEPT, C(2)) - matches SERVICES.DEPT.</summary>
        [MaxLength(2)]
        [Column(TypeName = "varchar(2)")]
        public string DEPT { get; set; }

        /// <summary>Legacy last-edit user (USERID, C(10)).</summary>
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string UserId { get; set; }

        /// <summary>Legacy last-edit date (UDATE, D).</summary>
        [Column(TypeName = "date")]
        public DateTime? UDate { get; set; }

        /// <summary>Legacy last-edit time (UTIME, C(5), HH:MM).</summary>
        [MaxLength(5)]
        [Column(TypeName = "varchar(5)")]
        public string UTime { get; set; }

        /// <summary>Legacy change flag (CHANGE, L -> 'Y'/'N').</summary>
        [MaxLength(1)]
        [Column(TypeName = "varchar(1)")]
        public string ChangeFlag { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Institution Company { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; }
    }
}
