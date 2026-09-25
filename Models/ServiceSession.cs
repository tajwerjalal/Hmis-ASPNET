using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    public class ServiceSession : Item
    {
        public int? SessionWeekday { get; set; }
        public DateTime? SessionDate { get; set; }
        public DateTime? SessionTime { get; set; }
        public DateTime? SessionAt { get; set; }
        public int StartingNo { get; set; }
        public int NumberIncrement { get; set; }
        public int MaxNo { get; set; }
        public bool ContinueNumbers { get; set; }

        public long? AfterSessionId { get; set; }
        public long? SessionNumberGeneratorId { get; set; }
        public long? BeforeSessionId { get; set; }

        public DateTime? StartingTime { get; set; }
        public DateTime? EndingTime { get; set; }
        public int? NumberOfDaysForAutomaticInstanceCreation { get; set; }
        public bool Refundable { get; set; }
        public int DisplayCount { get; set; }
        public double DisplayPercent { get; set; }
        public double Duration { get; set; }
        public int RoomNo { get; set; }
        public bool Deactivated { get; set; }
        public string DeactivateComment { get; set; }

        public long? OriginatingSessionId { get; set; }

        public int RecervedNumbers { get; set; }
        public bool PaidAppointmentsOnly { get; set; }
        public bool ExcludeFromPatientPortal { get; set; }
        public bool CanChangePatient { get; set; }
        public string SessionStartingNumber { get; set; }
        public string Activities { get; set; }
        public string Actions { get; set; }
        public string NotificationRoles { get; set; }
        public string DataEntryForms { get; set; }

        [ForeignKey("AfterSessionId")]
        public virtual ServiceSession AfterSession { get; set; }

        [ForeignKey("SessionNumberGeneratorId")]
        public virtual SessionNumberGenerator SessionNumberGenerator { get; set; }

        [ForeignKey("BeforeSessionId")]
        public virtual ServiceSession BeforeSession { get; set; }

        [ForeignKey("OriginatingSessionId")]
        public virtual ServiceSession OriginatingSession { get; set; }
    }
}
