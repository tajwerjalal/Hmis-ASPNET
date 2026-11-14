using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Ward master of the legacy hospital file [HMIS].[dbo].[WARDS], imported
    /// from E:\Projects\HMC\Master\wards.DBF (15 rows).
    ///
    /// Source fields and their widths:
    ///   WD_CODE C(2)  -> Code   varchar(10)  "PW", "GW", "IC" ...
    ///   NAME    C(35) -> Name   varchar(50)  "PRIVATE ROOM", "GENERAL WARD" ...
    ///   USERID  C(10) -> UserId varchar(15)  legacy stamp
    ///   UDATE   D     -> UDate  date         legacy stamp
    ///   UTIME   C(5)  -> UTime  varchar(5)   legacy stamp
    ///
    /// The beds of a ward live in <see cref="BedPosition"/> (legacy bed_pos.DBF)
    /// where the bed code is the ward code plus the bed number ("PW102").
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 13:45
    /// Change Log: 22-Oct-2026 13:45 - created from wards.DBF.
    /// </summary>
    [Table("WARD")]
    public class Ward
    {
        /// <summary>Surrogate key (the legacy ward code is not guaranteed unique
        /// across re-codes, and the beds point at it by code).</summary>
        [Key]
        public long Id { get; set; }

        /// <summary>Ward code, legacy WD_CODE ("PW", "GW", "IC" ...).</summary>
        [Required]
        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Code { get; set; }

        /// <summary>Ward name, legacy NAME ("PRIVATE ROOM" ...).</summary>
        [Column(TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>Last user who touched the row (legacy USERID).</summary>
        [Column(TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string UserId { get; set; }

        /// <summary>Last edit date (legacy UDATE, a real date in the file).</summary>
        [Column(TypeName = "date")]
        public DateTime? UDate { get; set; }

        /// <summary>Last edit time (legacy UTIME).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string UTime { get; set; }

        /// <summary>Soft-retire flag (app column).</summary>
        public bool Retired { get; set; }

        /// <summary>Why the ward was retired (app column).</summary>
        [Column(TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string RetireComments { get; set; }
    }
}
