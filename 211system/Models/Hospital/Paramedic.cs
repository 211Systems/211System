using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace _211system.Models.Hospital
{
    public class Paramedic
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LicenseNumber { get; set; }

        [MaxLength(50)]
        public string Specialization { get; set; }

        [Required]
        public string ParaAccountId { get; set; }
        [ForeignKey(nameof(ParaAccountId))]
        public virtual IdentityUser ParaAccount { get; set; }

        public Guid HospitalId { get; set; } // Zmieniona nazwa

        [ForeignKey(nameof(HospitalId))]
        public virtual Hospital Hospital { get; set; }
        public virtual ICollection<MedicalOperation> MedicalOperations { get; set; } = new List<MedicalOperation>();
        public virtual ICollection<GivenMedicine> GivenMedicines { get; set; } = new List<GivenMedicine>();
    }
}
