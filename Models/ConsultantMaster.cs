using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Legacy consultant register - [HMIS].[dbo].[CONSULT] (201 rows), the VFP
    /// master behind the consultant list every module uses.
    ///
    /// The table arrived from VFP with ten varchar(4000) columns and no key, so
    /// this migration gives it the real widths of the source file plus the two
    /// columns the application needs to manage a row safely:
    ///
    ///   PREFIX   C(5)  -> varchar(10)   "DR.", "PROF."
    ///   NAME     C(25) -> varchar(100)  consultant name (without prefix)
    ///   ID       C(3)  -> varchar(5)    consultant code (K01, S10, A02 ...)
    ///   TITLE    C(25) -> varchar(100)  speciality ("Paediatrician")
    ///   PHONE    C(16) -> varchar(50)
    ///   DIGREE   C(39) -> varchar(120)  degrees (legacy spelling kept)
    ///   DEPT     C(4)  -> varchar(4)    legacy department code (mostly empty)
    ///   USERID   C(7)  -> varchar(15)   last user
    ///   UDATE    C(8)  -> varchar(8)    last date, YYYYMMDD (legacy stamp)
    ///   UTIME    C(5)  -> varchar(5)    last time, HH:MM
    ///
    ///   RowId         -> bigint identity primary key (the legacy ID code is not
    ///                    unique: K04 appears twice, so it cannot be the key)
    ///   Retired       -> bit, soft-retire flag (true for every status but Active)
    ///   RetireComments-> varchar(500), reason of the retire
    ///   Status        -> varchar(20), Active / Inactive / Expired / Resigned /
    ///                    Retired / Other
    ///
    /// The app's own consultant entity (<see cref="Consultant"/>, a STAFF row
    /// with DTYPE='Consultant') already carries the same codes in STAFF.Code -
    /// 200 legacy consultants are linked that way - so the master screen writes
    /// both sides and nobody has to remember to copy a new consultant across.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 12:05
    /// Change Log: 22-Oct-2026 12:05 - created; legacy widths, identity key,
    ///             soft-retire columns, consultant master screen.
    ///             22-Oct-2026 13:05 - lifecycle Status column added.
    /// </summary>
    [Table("CONSULT")]
    public class ConsultantMaster
    {
        /// <summary>
        /// Surrogate key of the row (the legacy consultant code is not unique:
        /// K04 exists twice). It is called RowId and not Id because the legacy
        /// code column is ID and SQL Server compares column names without case.
        /// </summary>
        [Key]
        [Column("RowId")]
        public long RowId { get; set; }

        /// <summary>Honorific of the consultant (legacy PREFIX, C(5)).</summary>
        [Column("PREFIX", TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Prefix { get; set; }

        /// <summary>Consultant name without the honorific (legacy NAME, C(25)).</summary>
        [Column("NAME", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>Consultant code (legacy ID, C(3)) - matches STAFF.Code.</summary>
        [Column("ID", TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string Code { get; set; }

        /// <summary>Speciality / designation (legacy TITLE, C(25)).</summary>
        [Column("TITLE", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Title { get; set; }

        /// <summary>Phone numbers as typed in the legacy file (legacy PHONE, C(16)).</summary>
        [Column("PHONE", TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string Phone { get; set; }

        /// <summary>Degrees / qualifications (legacy DIGREE, C(39) - spelling kept).</summary>
        [Column("DIGREE", TypeName = "varchar(120)")]
        [MaxLength(120)]
        public string Degree { get; set; }

        /// <summary>Legacy department code (legacy DEPT, C(4)) - informational only.</summary>
        [Column("DEPT", TypeName = "varchar(4)")]
        [MaxLength(4)]
        public string Dept { get; set; }

        /// <summary>Last user who touched the row (legacy USERID, C(7)).</summary>
        [Column("USERID", TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string UserId { get; set; }

        /// <summary>Last edit date, YYYYMMDD (legacy UDATE, C(8)).</summary>
        [Column("UDATE", TypeName = "varchar(8)")]
        [MaxLength(8)]
        public string UDate { get; set; }

        /// <summary>Last edit time, HH:MM (legacy UTIME, C(5)).</summary>
        [Column("UTIME", TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string UTime { get; set; }

        /// <summary>
        /// Lifecycle status of the consultant (app column):
        /// Active / Inactive / Expired / Resigned / Retired / Other.
        /// Every status except Active also sets <see cref="Retired"/> so the
        /// existing "active only" filters and the app consultant stay correct.
        /// </summary>
        [Column(TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>Soft-retire flag (app column, not part of the legacy master).</summary>
        public bool Retired { get; set; }

        /// <summary>Why the consultant was retired (app column).</summary>
        [Column(TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string RetireComments { get; set; }
    }
}
