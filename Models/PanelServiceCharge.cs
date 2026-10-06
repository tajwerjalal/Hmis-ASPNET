using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// A panel/company rate for one service, per VFP department.
    ///
    /// VFP kept every department's company charges in a single compchg.DBF keyed by
    /// CID (company) + SCODE (service code) + DEPT. HMIS could only store charges that
    /// resolve to a billing ITEM, which today means ultrasound (INV-US-) and X-ray
    /// (INV-XR-); laboratory, dental, operation-theatre, hospitalisation and consultancy
    /// have no service master here yet. This table keeps the whole VFP catalogue so the
    /// Test Charges report can show every department, and links to ITEM when one exists.
    /// </summary>
    [Table("PANELSERVICECHARGE")]
    public class PanelServiceCharge
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>INSTITUTION id of the panel company (VFP compchg.CID).</summary>
        public long CompanyId { get; set; }

        /// <summary>VFP department code, e.g. "01" laboratory, "03" ultrasound.</summary>
        public string DepartmentCode { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>VFP service code, e.g. "03001".</summary>
        public string ServiceCode { get; set; } = string.Empty;

        /// <summary>Service description, denormalised from the VFP service master.</summary>
        public string ServiceName { get; set; } = string.Empty;

        public string ServiceGroup { get; set; }

        /// <summary>Company rate for this service (VFP compchg.SRATE).</summary>
        public double Rate { get; set; }

        /// <summary>Company foreigner rate (VFP compchg.URATE); null when not set.</summary>
        public double? ForeignerRate { get; set; }

        /// <summary>List rate from the VFP service master, for side-by-side comparison.</summary>
        public double? StandardRate { get; set; }

        public double? StandardForeignerRate { get; set; }

        /// <summary>
        /// Billing ITEM this charge applies to, when the service is live in HMIS.
        /// Null for departments whose service master has not been migrated yet.
        /// </summary>
        public long? ItemId { get; set; }

        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public long? RetirerId { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Institution Company { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }

    /// <summary>VFP department reference (dept.DBF).</summary>
    [Table("PANELDEPARTMENT")]
    public class PanelDepartment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BillOrder { get; set; }
        public bool Retired { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}