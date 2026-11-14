using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    /// <summary>
    /// ONE canonical department services master - [HMIS].[dbo].[SERVICES].
    /// Consolidation decision (22-Oct-2026, Tajwer Jalal): the department
    /// modules (X-ray today, US and others to follow) STOP owning private
    /// service tables. XRSERVICE (174 rows, a seeded re-copy of the same
    /// catalogue) was dropped; every row now lives here once, and each
    /// department's forms read/write this table filtered by the legacy
    /// DEPT column (e.g. '02' = X-RAY/CT SCAN SERVICES per MIG_DEPT.tsv).
    ///
    /// The master keeps its VFP shape - the 20 legacy columns (CODE, DESC,
    /// SRATE, URATE, DEPT, LINK, REQ, MAXDUR, MINDUR, PRES, GROUP, OLDCODE,
    /// SOURCE, CONREQ, REPORT, USERID, UDATE, UTIME, ACTIVE, CHANGE) are
    /// untouched; the app maps onto them and only ADDS the columns the
    /// ordering/retire features need (SNAME, PRIORITY, CATEGORYID, BILLABLE,
    /// RETIRED kit). Legacy declarations: CODE varchar(5), DESC varchar(200),
    /// SRATE/URATE numeric(18,2), DEPT varchar(5), PRES varchar(4000),
    /// GROUP varchar(5) (reserved word - bracketed), ACTIVE varchar(1)
    /// 'A'ctive / 'I'nactive, USERID + UDATE + UTIME = last-edit stamp
    /// (user / date / HH:mm time).
    ///
    /// Column mapping used by EF
    ///   Name          -> DESC     (the service display name)
    ///   Sname         -> SNAME    (short name; also the search helper)
    ///   Rate          -> SRATE    (standard rate)
    ///   RateForeigner -> URATE    (foreigner rate; legacy 1.5x rule lives here)
    ///   Requirements  -> PRES     (preparation/instruction text, 4000 chars)
    ///   Priority      -> PRIORITY (app-added smallint)
    ///   CategoryId    -> CATEGORYID (FK XRSERVICECATEGORY; legacy GROUP stays
    ///                                untouched for the VFP-era tools)
    ///   StampUser/StampDate/StampTime -> USERID / UDATE / UTIME
    ///
    /// XRSERVICE-Bak: workspace/temp/services_consolidation.sql keeps
    /// XRSERVICE_BAK_20261021 and SERVICES_BAK_20261021 (pre-merge copies).
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 16:35
    /// Change Log: 22-Oct-2026 16:35 - created as part of the SERVICES
    ///             consolidation (drop XRSERVICE).
    /// </summary>
    [Table("SERVICES")]
    public class DeptService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Service display name - legacy DESC varchar(200).</summary>
        [Column("DESC")]
        public string Name { get; set; }

        /// <summary>5-char legacy service code (the item master mirror key).</summary>
        [Column("CODE")]
        public string Code { get; set; }

        /// <summary>Short/alias name for search and compact grids (app-added).</summary>
        [Column("SNAME")]
        public string Sname { get; set; }

        /// <summary>Standard rate - legacy SRATE numeric(18,2).</summary>
        [Column("SRATE")]
        public decimal? Rate { get; set; }

        /// <summary>Foreigner rate - legacy URATE numeric(18,2).</summary>
        [Column("URATE")]
        public decimal? RateForeigner { get; set; }

        /// <summary>Legacy department code (varchar 5, e.g. '02' x-ray).</summary>
        [Column("DEPT")]
        public string Dept { get; set; }

        /// <summary>Preparation/instruction text - legacy PRES varchar(4000).</summary>
        [Column("PRES")]
        public string Requirements { get; set; }

        /// <summary>Ordering priority hint for the ordering screens (app-added).</summary>
        [Column("PRIORITY")]
        public Priority? Priority { get; set; }

        /// <summary>Legacy duration hints - MINDUR/MAXDUR varchar(10) columns.</summary>
        [Column("MINDUR")]
        public string MinDuration { get; set; }

        [Column("MAXDUR")]
        public string MaxDuration { get; set; }

        /// <summary>
        /// Legacy consultant-required flag - CONREQ varchar(1): 'Y' required,
        /// 'N' not required, 'O' optional (VFP-era vocabulary).
        /// </summary>
        [Column("CONREQ")]
        public string ConsultantRequired { get; set; }

        /// <summary>FK XRSERVICECATEGORY (X-ray/US categories, app-added).</summary>
        [Column("CATEGORYID")]
        public long? CategoryId { get; set; }

        /// <summary>Orderable/billable flag (app-added; 1 = can be indented).</summary>
        [Column("BILLABLE")]
        public bool Billable { get; set; } = true;

        /// <summary>'A' active /'I' inactive (legacy ACTIVE varchar(1)).</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; } = true;

        // ---- retire kit (app-added; reuses the legacy last-edit stamp) ----

        [Column("RETIRED")]
        public bool Retired { get; set; }

        [Column("RETIREDAT")]
        public DateTime? RetiredAt { get; set; }

        [MaxLength(200)]
        [Column("RETCOMMENTS")]
        public string RetireComments { get; set; }

        /// <summary>Last-edit user name - legacy USERID varchar(50) stamp.</summary>
        [Column("USERID")]
        public string StampUser { get; set; }

        /// <summary>Last-edit date - legacy UDATE datetime stamp.</summary>
        [Column("UDATE")]
        public DateTime? StampDate { get; set; }

        /// <summary>Last-edit time 'HH:mm' - legacy UTIME varchar(5) stamp.</summary>
        [Column("UTIME")]
        public string StampTime { get; set; }

        /// <summary>Category navigation (XRSERVICECATEGORY).</summary>
        [ForeignKey("CategoryId")]
        public virtual XrServiceCategory Category { get; set; }
    }
}
