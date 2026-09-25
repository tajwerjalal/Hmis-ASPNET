using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("PATIENTENCOUNTER")]
    public class PatientEncounter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public EncounterType? EncounterType { get; set; }
        public string EncounterComments { get; set; }
        public DateTime? EncounterDate { get; set; }
        public DateTime? EncounterTime { get; set; }
        public DateTime? DateOfAdmission { get; set; }
        public DateTime? TimeOfAdmission { get; set; }
        public DateTime? DateOfDischarge { get; set; }
        public DateTime? TimeOfDischarge { get; set; }
        public string Name { get; set; }
        public string BhtNo { get; set; }
        public string BhtLong { get; set; }
        public bool Discharged { get; set; }
        public bool PaymentFinalized { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public AdmissionType? AdmissionType { get; set; }
        public long? PatientId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditerId { get; set; }
        public long? RetirerId { get; set; }
        public long? ReferringDepartmentId { get; set; }
        public long? ReferringInstitutionId { get; set; }
        public long? ReferringStaffId { get; set; }
        public long? ReferringConsultantId { get; set; }
        public long? OpdDoctorId { get; set; }
        public long? WorkplaceId { get; set; }
        public long? GuardianId { get; set; }
        public long? GuardianRelationshipToPatientId { get; set; }
        public long? CreditCompanyId { get; set; }
        public long? FinalBillId { get; set; }
        public string ReferralId { get; set; }
        public string ReferenceNo { get; set; }
        public string PolicyNo { get; set; }
        public bool Foriegner { get; set; }
        public bool Claimable { get; set; }
        public double? CreditLimit { get; set; }
        public double? CreditUsedAmount { get; set; }
        public double? CreditPaidAmount { get; set; }
        public double? GrantTotal { get; set; }
        public double? Discount { get; set; }
        public double? NetTotal { get; set; }
        public double? AdjustedTotal { get; set; }
        public double? Weight { get; set; }
        public double? Height { get; set; }
        public double? Bmi { get; set; }
        public int? Sbp { get; set; }
        public int? Dbp { get; set; }
        public int? Pr { get; set; }
        public int? RespiratoryRate { get; set; }
        public int? Saturation { get; set; }
        public int? Pfr { get; set; }
        public string ChiefComplaint { get; set; }
        public string HistoryOfPresentingComplaint { get; set; }
        public string PastMedicalHistory { get; set; }
        public string Examination { get; set; }
        public string Diagnosis { get; set; }
        public string Investigations { get; set; }
        public string TreatmentPlan { get; set; }
        public string Management { get; set; }
        public string FollowUp { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("ReferringDepartmentId")]
        public virtual Department ReferringDepartment { get; set; }
        [ForeignKey("ReferringInstitutionId")]
        public virtual Institution ReferringInstitution { get; set; }
        [ForeignKey("ReferringStaffId")]
        public virtual Staff ReferringStaff { get; set; }
        [ForeignKey("ReferringConsultantId")]
        public virtual Staff ReferringConsultant { get; set; }
        [ForeignKey("OpdDoctorId")]
        public virtual Staff OpdDoctor { get; set; }
        [ForeignKey("WorkplaceId")]
        public virtual Institution Workplace { get; set; }
        [ForeignKey("GuardianId")]
        public virtual Person Guardian { get; set; }
        [ForeignKey("GuardianRelationshipToPatientId")]
        public virtual Category GuardianRelationshipToPatient { get; set; }
        [ForeignKey("CreditCompanyId")]
        public virtual Institution CreditCompany { get; set; }
        [ForeignKey("FinalBillId")]
        public virtual Bill FinalBill { get; set; }
    }
}