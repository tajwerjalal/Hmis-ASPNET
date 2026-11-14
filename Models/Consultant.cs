using System.ComponentModel.DataAnnotations;

namespace HmisWeb.Models
{
    /// <summary>
    /// Consultant master (TPH row of [STAFF]).
    ///
    /// The fee / hospital-charge columns of the legacy [STAFF] table are
    /// nullable, so the three money properties are nullable here as well. A
    /// consultant that has no rate card yet simply has no fee; screens must
    /// coalesce to 0 when they display or bill the value.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 10:30
    /// Change Log: 23-Oct-2026 - fees made nullable to match [STAFF] and stop
    ///             the "Nullable object must have a value" materialisation error.
    ///             24-Oct-2026 - evening clinic window added (the legacy
    ///             SessionStartTime / SessionEndTime are the morning window).
    /// </summary>
    public class Consultant : Doctor
    {
        public string Degrees { get; set; }

        public double? FirstVisitFee { get; set; }

        public double? SubsequentVisitFee { get; set; }

        public string WorkDays { get; set; }

        /// <summary>Morning clinic start, HH:mm.</summary>
        public string SessionStartTime { get; set; }

        /// <summary>Morning clinic end, HH:mm.</summary>
        public string SessionEndTime { get; set; }

        /// <summary>Evening clinic start, HH:mm.</summary>
        [MaxLength(50)]
        public string EveningStartTime { get; set; }

        /// <summary>Evening clinic end, HH:mm.</summary>
        [MaxLength(50)]
        public string EveningEndTime { get; set; }

        public string DefaultRoomNo { get; set; }

        public double? DefaultHospitalFee { get; set; }
    }
}
