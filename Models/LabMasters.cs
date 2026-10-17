using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// A laboratory "test data" row (a measurable parameter of a test, with its unit and
    /// normal range). Migrated from the VFP Labsys table <c>readings.dbf</c>; the normal
    /// ranges held in <c>nrange.dbf</c> are summarised in <see cref="Range"/>.
    /// </summary>
    [Table("TESTDATA")]
    public class TestData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Range { get; set; }

        public long? CategoryId { get; set; }
        public int OrderNo { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }
    }

    /// <summary>
    /// A canned remark for a laboratory test. Migrated from VFP Labsys <c>tsremark.dbf</c>.
    /// </summary>
    [Table("TESTREMARK")]
    public class TestRemark
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? ItemId { get; set; }
        public string TestCode { get; set; }
        public string Remarks { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }

        [ForeignKey(nameof(ItemId))]
        public virtual Item Item { get; set; }
    }

    /// <summary>
    /// A growth description used when reporting cultures. Migrated from VFP Labsys
    /// <c>grremark.dbf</c>.
    /// </summary>
    [Table("GROWTHDESCRIPTION")]
    public class GrowthDescription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; }
        public string Description { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
    }

    /// <summary>
    /// A micro-organism used when reporting cultures. Migrated from VFP Labsys
    /// <c>organisms.dbf</c>.
    /// </summary>
    [Table("ORGANISM")]
    public class Organism
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; }
        public string Name { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
    }
}