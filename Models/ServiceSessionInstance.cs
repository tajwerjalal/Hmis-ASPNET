using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("SERVICESESESSIONINSTANCE")]
    public class ServiceSessionInstance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double? Total { get; set; }
        public double? TotalForForeigner { get; set; }
        public bool? DiscountAllowed { get; set; }

        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? SpecialityId { get; set; }
        public long? StaffId { get; set; }
        public long? ForInstitutionId { get; set; }
        public long? ForDepartmentId { get; set; }

        public BillType? ForBillType { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Tname { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string PrintName { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        public InwardChargeType? InwardChargeType { get; set; }
        public double DblValue { get; set; }
        public SessionNumberType? SessionNumberType { get; set; }
        public bool PriceByBatch { get; set; }
        public bool Billable { get; set; }
        public bool Vatable { get; set; }
        public bool Formatable { get; set; }
        public bool PatientNotRequired { get; set; }
        public bool ChargesVisibleForInward { get; set; }
        public bool RequestForQuentity { get; set; }
        public bool MarginNotAllowed { get; set; }
        public double VatPercentage { get; set; }
        public bool ScanFee { get; set; }
        public double ProfitMargin { get; set; }
        public string CreditNumbers { get; set; }
        public string CashNumbers { get; set; }
        public string AgencyNumbers { get; set; }
        public string ReserveName { get; set; }
        public string ReserveNumbers { get; set; }
        public int MaxTableRows { get; set; }

        public int? SessionWeekday { get; set; }
        public DateTime? SessionDate { get; set; }
        public DateTime? SessionTime { get; set; }
        public DateTime? SessionAt { get; set; }
        public int StartingNo { get; set; }
        public int NumberIncrement { get; set; }
        public int MaxNo { get; set; }
        public bool ContinueNumbers { get; set; }

        public long? SessionNumberGeneratorId { get; set; }
        public DateTime? StartingTime { get; set; }
        public DateTime? EndingTime { get; set; }
        public bool Refundable { get; set; }
        public int DisplayCount { get; set; }
        public double DisplayPercent { get; set; }
        public double Duration { get; set; }
        public int RoomNo { get; set; }
        public bool Deactivated { get; set; }
        public string DeactivateComment { get; set; }

        public long? OriginatingSessionId { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("ForInstitutionId")]
        public virtual Institution ForInstitution { get; set; }

        [ForeignKey("ForDepartmentId")]
        public virtual Department ForDepartment { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("SessionNumberGeneratorId")]
        public virtual SessionNumberGenerator SessionNumberGenerator { get; set; }

        [ForeignKey("OriginatingSessionId")]
        public virtual ServiceSession OriginatingSession { get; set; }
    }
}
