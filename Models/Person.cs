using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("PERSON")]
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Nic { get; set; }
        public string Address { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Mobile { get; set; }
        public string Phone { get; set; }

        [Column("TNAME")]
        public string FullName { get; set; }

        [Column("SNAME")]
        public string NameWithInitials { get; set; }

        public string Initials { get; set; }
        public string Surname { get; set; }
        public string LastName { get; set; }
        public string ZoneCode { get; set; }
        public DateTime? Dob { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? AreaId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public Title? Title { get; set; }
        public Sex? Sex { get; set; }
        public bool Foreigner { get; set; }
        public long? CivilStatusId { get; set; }
        public long? RaceId { get; set; }
        public long? BloodGroupId { get; set; }
        public long? OccupationId { get; set; }
        public long? ReligionId { get; set; }
        public long? MembershipSchemeId { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("AreaId")]
        public virtual Area Area { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("CivilStatusId")]
        public virtual Item CivilStatus { get; set; }

        [ForeignKey("RaceId")]
        public virtual Item Race { get; set; }

        [ForeignKey("BloodGroupId")]
        public virtual Item BloodGroup { get; set; }

        [ForeignKey("OccupationId")]
        public virtual Item Occupation { get; set; }

        [ForeignKey("ReligionId")]
        public virtual Item Religion { get; set; }

        [ForeignKey("MembershipSchemeId")]
        public virtual MembershipScheme MembershipScheme { get; set; }

        public virtual WebUser WebUser { get; set; }

        public string GetAgeString()
        {
            if (Dob == null) return "";
            var age = DateTime.Now - Dob.Value;
            int years = age.Days / 365;
            int months = (age.Days % 365) / 30;
            int days = (age.Days % 365) % 30;
            if (years > 0) return $"{years}y {months}m {days}d";
            if (months > 0) return $"{months}m {days}d";
            return $"{days}d";
        }
    }
}