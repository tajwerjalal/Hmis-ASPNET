using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Models.Pharmacy;

namespace HmisWeb.Models
{
    /// <summary>
    /// An operating theatre (OT room). Mid-size hospitals run a handful of theatres -
    /// major, minor and day-care - and the day board / register / utilisation reports
    /// are all keyed off this master.
    /// </summary>
    [Table("OTTHEATRE")]
    public class OtTheatre
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [StringLength(30)]
        public string Code { get; set; } = "";

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        /// <summary>Major / Minor / Day Care / Emergency.</summary>
        [StringLength(30)]
        public string TheatreType { get; set; } = "Major";

        [StringLength(150)]
        public string Location { get; set; } = "";

        public bool Active { get; set; } = true;

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = "";
    }

    /// <summary>
    /// One planned / performed operation. Carries the booking (who, which theatre,
    /// when) and - once the case is done - the operation note (times, procedure
    /// performed, findings, outcome, anaesthesia, ASA grade, post-op order).
    /// Charges are NOT raised here: services are billed through the existing
    /// department indent / cashier flow, so this table only references the bill
    /// number for reconciliation.
    /// </summary>
    [Table("OTCASE")]
    public class OtCase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>OT-20261020-001 style running number for the day.</summary>
        [StringLength(30)]
        public string CaseNo { get; set; } = "";

        public long PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        public long? TheatreId { get; set; }
        [ForeignKey("TheatreId")]
        public virtual OtTheatre Theatre { get; set; }

        public long? SurgeonId { get; set; }
        [ForeignKey("SurgeonId")]
        public virtual Consultant Surgeon { get; set; }

        public long? AnesthetistId { get; set; }
        [ForeignKey("AnesthetistId")]
        public virtual Staff Anesthetist { get; set; }

        /// <summary>Planned procedure, taken from the service (ITEM) master.</summary>
        public long? ProcedureItemId { get; set; }
        [ForeignKey("ProcedureItemId")]
        public virtual Item ProcedureItem { get; set; }

        [StringLength(250)]
        public string ProcedurePlanned { get; set; } = "";

        /// <summary>Elective / Emergency.</summary>
        [StringLength(20)]
        public string CaseType { get; set; } = "Elective";

        public DateTime ScheduledDate { get; set; } = DateTime.Today;

        /// <summary>Scheduled start time as HH:mm.</summary>
        [StringLength(10)]
        public string SlotTime { get; set; } = "";

        public int DurationMins { get; set; }

        /// <summary>Scheduled / In Theatre / Completed / Cancelled / Postponed.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";

        /// <summary>None / Local / Sedation / GA / Spinal / Epidural / Regional / Combined.</summary>
        [StringLength(20)]
        public string AnesthesiaPlanned { get; set; } = "None";
        [StringLength(20)]
        public string AnesthesiaGiven { get; set; } = "";

        /// <summary>ASA physical status grade I - VI.</summary>
        [StringLength(10)]
        public string AsaGrade { get; set; } = "";

        /// <summary>Not Done / Fit / Fit With Caution / Unfit (pre-anaesthesia check).</summary>
        [StringLength(30)]
        public string Fitness { get; set; } = "Not Done";

        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }

        [StringLength(250)]
        public string ProcedurePerformed { get; set; } = "";
        [StringLength(500)]
        public string Findings { get; set; } = "";
        /// <summary>Uneventful / Complication / Death.</summary>
        [StringLength(20)]
        public string Outcome { get; set; } = "";
        [StringLength(500)]
        public string Complications { get; set; } = "";
        [StringLength(500)]
        public string PostOpInstructions { get; set; } = "";

        [StringLength(250)]
        public string CancelReason { get; set; } = "";

        /// <summary>Bill / receipt raised through the cashier for this case (reference only).</summary>
        [StringLength(50)]
        public string BillNo { get; set; } = "";

        [StringLength(500)]
        public string Remarks { get; set; } = "";

        public long? InstitutionId { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        public long? DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = "";

        public virtual ICollection<OtCaseItem> Items { get; set; } = new List<OtCaseItem>();

        /// <summary>Minutes the case actually occupied the theatre, from the recorded times.</summary>
        public int? ActualMinutes
        {
            get
            {
                if (ActualStart.HasValue && ActualEnd.HasValue && ActualEnd.Value > ActualStart.Value)
                    return (int)(ActualEnd.Value - ActualStart.Value).TotalMinutes;
                return null;
            }
        }
    }

    /// <summary>Consumables / implants used in a case (recorded for the OT register and stock follow-up).</summary>
    [Table("OTCASEITEM")]
    public class OtCaseItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long CaseId { get; set; }
        [ForeignKey("CaseId")]
        public virtual OtCase Case { get; set; }

        /// <summary>
        /// Optional link to the pharmacy stock master (PHITEM) - sutures, gloves,
        /// implants and other theatre consumables are stock, not billable services.
        /// Lines may also be typed free-text when the item is not in the stock master.
        /// </summary>
        public int? ItemId { get; set; }
        [ForeignKey("ItemId")]
        public virtual PhItem Item { get; set; }

        [Required, StringLength(150)]
        public string ItemName { get; set; } = "";

        public decimal Qty { get; set; } = 1;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }

        /// <summary>Whether the line is billed to the patient / panel.</summary>
        public bool Chargeable { get; set; }

        [StringLength(200)]
        public string Remarks { get; set; } = "";
    }
}
