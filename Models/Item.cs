using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("ITEM")]
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Tname { get; set; }
        public string Code { get; set; }
        public string CodeSystem { get; set; }
        public string Barcode { get; set; }
        public long? LastBarcode { get; set; }
        public string PrintName { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }
        public string Descreption { get; set; }
        public string Comments { get; set; }

        public int OrderNo { get; set; }
        public bool CanSechduleForOtherDays { get; set; }
        public long? ItemId { get; set; }
        public bool IsMasterItem { get; set; }
        public bool HasReportFormat { get; set; }
        public int NumberOfDaysToMarkAsShortExpiary { get; set; }

        public double? Total { get; set; }
        public double? TotalForForeigner { get; set; }
        public bool? DiscountAllowed { get; set; }

        public double? StaffRate { get; set; }
        public double? UnitRate { get; set; }
        public string Requirements { get; set; }
        public bool? ConsultantRequired { get; set; }
        public string MinDuration { get; set; }
        public string MaxDuration { get; set; }
        public string Source { get; set; }
        public bool? CorporateActive { get; set; }

        public long? CategoryId { get; set; }
        public long? FinancialCategoryId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? SpecialityId { get; set; }
        public long? StaffId { get; set; }
        public long? ForInstitutionId { get; set; }
        public long? ForDepartmentId { get; set; }
        public long? BilledAsId { get; set; }
        public long? ReportedAsId { get; set; }
        public long? MasterItemReferenceId { get; set; }
        public long? MeasurementUnitId { get; set; }
        public long? WorksheetId { get; set; }
        public long? ReportFormatId { get; set; }
        public long? ManufacturerId { get; set; }
        public long? ImporterId { get; set; }
        public long? ParentItemId { get; set; }
        public long? VmpId { get; set; }
        public long? MachineId { get; set; }
        public long? StrengthUnitId { get; set; }
        public long? BaseUnitId { get; set; }
        public long? IssueUnitId { get; set; }

        public BillType? ForBillType { get; set; }
        public ItemType? ItemType { get; set; }
        public InwardChargeType? InwardChargeType { get; set; }
        public SymanticType? SymanticType { get; set; }
        public DepartmentType? DepartmentType { get; set; }
        public SessionNumberType? SessionNumberType { get; set; }
        public Priority? Priority { get; set; }
        public ItemBarcodeGenerationStrategy? ItemBarcodeGenerationStrategy { get; set; }

        public bool UserChangable { get; set; }
        public double DblValue { get; set; }
        public bool PriceByBatch { get; set; }
        public bool Billable { get; set; }
        public bool Vatable { get; set; }
        public bool Formatable { get; set; }
        public bool PatientNotRequired { get; set; }
        public bool ChargesVisibleForInward { get; set; }
        public bool RequestForQuentity { get; set; }
        public bool MarginNotAllowed { get; set; }
        public bool PrintSessionNumber { get; set; }
        public bool Inactive { get; set; }
        public double VatPercentage { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool ScanFee { get; set; }
        public double ProfitMargin { get; set; }
        public string CreditNumbers { get; set; }
        public string CashNumbers { get; set; }
        public string AgencyNumbers { get; set; }
        public string ReserveName { get; set; }
        public string ReserveNumbers { get; set; }
        public int MaxTableRows { get; set; }
        public bool HasMoreThanOneComponant { get; set; }
        public double? IssueUnitsPerPackUnit { get; set; }
        public double? BaseUnitsPerIssueUnit { get; set; }
        public bool PrintFeesForBills { get; set; }
        public bool CanRemoveItemfromPackage { get; set; }
        public string ForGender { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [Column("DTYPE")]
        public string Discriminator { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("FinancialCategoryId")]
        public virtual Category FinancialCategory { get; set; }

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

        [ForeignKey("BilledAsId")]
        public virtual Item BilledAs { get; set; }

        [ForeignKey("ReportedAsId")]
        public virtual Item ReportedAs { get; set; }

        [ForeignKey("MasterItemReferenceId")]
        public virtual Item MasterItemReference { get; set; }

        [ForeignKey("ParentItemId")]
        public virtual Item ParentItem { get; set; }

        [ForeignKey("MeasurementUnitId")]
        public virtual MeasurementUnit MeasurementUnit { get; set; }

        [ForeignKey("WorksheetId")]
        public virtual Category Worksheet { get; set; }

        [ForeignKey("ReportFormatId")]
        public virtual Category ReportFormat { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Institution Manufacturer { get; set; }

        [ForeignKey("ImporterId")]
        public virtual Institution Importer { get; set; }

        [ForeignKey("VmpId")]
        public virtual Vmp Vmp { get; set; }

        [ForeignKey("MachineId")]
        public virtual Machine Machine { get; set; }

        [ForeignKey("StrengthUnitId")]
        public virtual MeasurementUnit StrengthUnit { get; set; }

        [ForeignKey("BaseUnitId")]
        public virtual MeasurementUnit BaseUnit { get; set; }

        [ForeignKey("IssueUnitId")]
        public virtual MeasurementUnit IssueUnit { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}