using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _211system.Models.Hospital
{
    public class HospitalWard
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public Guid HospitalId { get; set; }

        [ForeignKey(nameof(HospitalId))]
        public virtual Hospital Hospital { get; set; }

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
