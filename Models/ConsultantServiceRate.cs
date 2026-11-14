using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Consultant rate card - [HMIS].[dbo].[CONSULTANTSERVICERATE].
    ///
    /// The rows are loaded from the legacy file E:\Projects\HMC\Master\con_rat.DBF
    /// (10,461 live rows, last written 31-Aug-1999), which carries one charge per
    /// company + consultant + ward class for the consultation service:
    ///
    ///   CMP_CD  C(4)   -> CompanyCode    varchar(5)   COMPANY.ID (A001 ... )
    ///   CM_CD   C(3)   -> ConsultantCode varchar(10)  STAFF.Code / CONSULT.ID
    ///   WD_CD   C(1)   -> WardCode       varchar(2)   WCODES.CODE (G, P, I ...)
    ///   RT_FLG  C(1)   -> RateFlag       varchar(1)   legacy V / F flag
    ///   RATE_1  N(8,2) -> StaffRate      decimal(8,2) legacy first amount
    ///   RATE_2  N(8,2) -> UnitRate       decimal(8,2) legacy second amount
    ///   SCODE   C(5)   -> ServiceCode    varchar(5)   always 11001 ("Visit (s)")
    ///   USERID / UDATE / UTIME          legacy stamp (blank in the source file)
    ///
    /// ConsultantId is the app consultant (STAFF with DTYPE='Consultant'). It is
    /// optional because 8 of the 108 codes in con_rat no longer exist in STAFF
    /// (A16, A21, I03, J02, M09, N08, N09, V04); those rows keep the legacy code
    /// in ConsultantCode and are shown by code.
    ///
    /// The money columns used to be float; the legacy compchg/consultant rates
    /// are money values, so they are decimal(8,2) (same rule as
    /// COMPANYSERVICERATE).
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 14:40
    /// Change Log: 22-Oct-2026 14:40 - legacy con_rat columns added (company /
    ///             consultant code / ward class / service / rate flag / stamp),
    ///             ConsultantId made optional, money columns converted to
    ///             decimal(8,2).
    /// </summary>
    [Table("CONSULTANTSERVICERATE")]
    public class ConsultantServiceRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// App consultant (STAFF id). Null on the legacy rows whose consultant
        /// code is not present in the staff master any more.
        /// </summary>
        public long? ConsultantId { get; set; }

        /// <summary>Legacy consultant code (con_rat.CM_CD), always filled.</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string ConsultantCode { get; set; }

        /// <summary>Billing item of the consultation service (11001 = Visit(s)).</summary>
        [Required]
        public long ItemId { get; set; }

        /// <summary>Legacy RATE_1 - first amount of the rate pair.</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal StaffRate { get; set; }

        /// <summary>Legacy RATE_2 - second amount of the rate pair.</summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal UnitRate { get; set; }

        public long? DepartmentId { get; set; }

        /// <summary>Company code (legacy con_rat.CMP_CD, COMPANY.ID).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string CompanyCode { get; set; }

        /// <summary>Ward class code (legacy con_rat.WD_CD, WCODES.CODE).</summary>
        [Column(TypeName = "varchar(2)")]
        [MaxLength(2)]
        public string WardCode { get; set; }

        /// <summary>Service code (legacy con_rat.SCODE - 11001 in the file).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string ServiceCode { get; set; }

        /// <summary>Legacy RT_FLG: V or F.</summary>
        [Column(TypeName = "varchar(1)")]
        [MaxLength(1)]
        public string RateFlag { get; set; }

        /// <summary>Last user (legacy USERID stamp, blank in con_rat).</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string UserId { get; set; }

        /// <summary>Last edit date (legacy UDATE stamp).</summary>
        [Column(TypeName = "date")]
        public DateTime? UDate { get; set; }

        /// <summary>Last edit time 'HH:mm' (legacy UTIME stamp).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string UTime { get; set; }

        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("ConsultantId")]
        public virtual Consultant Consultant { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}
