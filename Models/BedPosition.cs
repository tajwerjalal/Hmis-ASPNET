using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Bed position of the legacy hospital file [HMIS].[dbo].[BED_POS], imported
    /// from E:\Projects\HMC\Master\bed_pos.DBF (113 live rows).
    ///
    /// Source fields and their widths:
    ///   WD_CD   C(5)  -> BedCode     varchar(10)  ward code + bed no ("PW102")
    ///   DESC    C(25) -> Description varchar(50)  "PRIVATE ROOM", "GENERAL WARD"
    ///   VO_FL   C(1)  -> Status      varchar(10)  'O' occupied, else Vacant
    ///   PAT_CD  C(7)  -> PatientCode varchar(20)  patient staying in the bed
    ///   SCODE   C(5)  -> ServiceCode varchar(10)  room-charge service code
    ///   USERID  C(10) -> UserId      varchar(15)  legacy stamp
    ///   UDATE   D     -> UDate       date         legacy stamp
    ///   UTIME   C(5)  -> UTime       varchar(5)   legacy stamp
    ///
    /// The ward of the bed is stored both as the code (<see cref="WardCode"/>,
    /// the first two characters of WD_CD) and as a reference to [WARD], so the
    /// screen can filter and count beds per ward without parsing strings.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 13:45
    /// Change Log: 22-Oct-2026 13:45 - created from bed_pos.DBF.
    /// </summary>
    [Table("BED_POS")]
    public class BedPosition
    {
        /// <summary>Surrogate key.</summary>
        [Key]
        public long Id { get; set; }

        /// <summary>Ward code the bed belongs to (legacy first two characters of WD_CD).</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string WardCode { get; set; }

        /// <summary>Bed code, legacy WD_CD ("PW102", "GW423", "OT" ...).</summary>
        [Required]
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string BedCode { get; set; }

        /// <summary>Description inherited from the ward (legacy DESC).</summary>
        [Column(TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string Description { get; set; }

        /// <summary>Vacant or Occupied (legacy VO_FL: 'O' = occupied).</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Status { get; set; } = "Vacant";

        /// <summary>Patient staying in the bed (legacy PAT_CD).</summary>
        [Column(TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string PatientCode { get; set; }

        /// <summary>Room-charge service code of the bed (legacy SCODE).</summary>
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string ServiceCode { get; set; }

        /// <summary>Last user who touched the row (legacy USERID).</summary>
        [Column(TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string UserId { get; set; }

        /// <summary>Last edit date (legacy UDATE).</summary>
        [Column(TypeName = "date")]
        public DateTime? UDate { get; set; }

        /// <summary>Last edit time (legacy UTIME).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string UTime { get; set; }

        /// <summary>Soft-retire flag (app column).</summary>
        public bool Retired { get; set; }

        /// <summary>Why the bed was retired (app column).</summary>
        [Column(TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string RetireComments { get; set; }
    }
}
