using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _211system.Models.Hospital
{
    public class Patient
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(11)]
        public string Pesel { get; set; }

        
        [MaxLength(250)]
        public string Condition { get; set; }

        [Required]
        public Guid IncidentId { get; set; }

        // [ForeignKey(nameof(IncidentId))]
        // public virtual Incident Incident { get; set; }
        public Guid? HospitalWardId { get; set; }

        [ForeignKey(nameof(HospitalWardId))]
        public virtual HospitalWard HospitalWard { get; set; }
    }
}
