using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// One panel-company transfer of an inpatient / outpatient stay, migrated
    /// from the legacy VFP file [HMIS].[dbo].[CMPTRANS] (company transfer):
    ///
    ///   TYPE       C(1)   'I' = inpatient, 'O' = outpatient
    ///   REGNO      C(8)   Registration number (the BHT of the stay)
    ///   CIDOLD     C(4)   Company the patient was registered with
    ///   CID        C(4)   Company it was transferred to
    ///   ECODE      C(8)   Employee code of the company
    ///   ERelATION  C(15)  Relation of the patient to the employee
    ///   REFNO      C(15)  Reference / policy number
    ///   REFVALID   C(8)   Reference valid upto (date)
    ///   CARDNO     C(15)  Company card number
    ///   BEDENTITLE C(5)   Bed entitlement ward code
    ///   UPDIND     L      Update the rates in all the indents of the stay
    ///   KEY        C(30)  Transfer key of the transaction
    ///
    /// The application keeps what the transfer screens actually need: the
    /// patient / type / registration number, the two companies, the employee
    /// and card, the bed entitlement and the "update rates in all indents"
    /// flag, plus the soft-retire (with reason) of the transfer. The legacy
    /// movement stamps are kept alongside the app stamps.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 16:25
    /// Change Log: 22-Oct-2026 - created from the cmptrans code of the Company
    ///             Transfer screen (cmptrans.scx).
    /// </summary>
    [Table("COMPANYTRANSFER")]
    public class CompanyTransfer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Movement type: 'I' = inpatient, 'O' = outpatient.</summary>
        [Column(TypeName = "varchar(1)")]
        [MaxLength(1)]
        public string Type { get; set; } = "I";

        /// <summary>Registration number (BHT) of the stay.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string RegNo { get; set; }

        public long? PatientEncounterId { get; set; }

        public long? FromCompanyId { get; set; }

        public long? ToCompanyId { get; set; }

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }

        /// <summary>Company the patient was registered with.</summary>
        [ForeignKey("FromCompanyId")]
        public virtual Institution FromCompany { get; set; }

        [ForeignKey("ToCompanyId")]
        public virtual Institution ToCompany { get; set; }

        [Column(TypeName = "varchar(4)")]
        [MaxLength(4)]
        public string FromCompanyCode { get; set; }

        [Column(TypeName = "varchar(4)")]
        [MaxLength(4)]
        public string ToCompanyCode { get; set; }

        /// <summary>Employee code of the panel.</summary>
        [Column(TypeName = "varchar(8)")]
        [MaxLength(8)]
        public string EmployeeCode { get; set; }

        /// <summary>Employee name at the transfer time.</summary>
        [Column(TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string EmployeeName { get; set; }

        /// <summary>Designation of the employee.</summary>
        [Column(TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string EmployeeDesignation { get; set; }

        /// <summary>Relation of the patient to the employee.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string EmployeeRelation { get; set; }

        /// <summary>Reference / policy number.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string ReferenceNo { get; set; }

        /// <summary>Reference valid upto (date).</summary>
        [Column(TypeName = "date")]
        public DateTime? ReferenceValid { get; set; }

        /// <summary>Company card number.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string CardNo { get; set; }

        /// <summary>Bed entitlement ward code of the new company.</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string BedEntitlement { get; set; }

        /// <summary>True when the rates of all the indents must be updated.</summary>
        public bool UpdateRates { get; set; }

        public DateTime? TransferDate { get; set; }

        public bool Retired { get; set; }

        [Column(TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string RetireComments { get; set; }

        public long? CreaterId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public long? EditerId { get; set; }

        public DateTime? EditedAt { get; set; }

        public long? RetirerId { get; set; }

        public DateTime? RetiredAt { get; set; }

        public string UserId { get; set; }

        public DateTime? UDate { get; set; }

        public string UTime { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
