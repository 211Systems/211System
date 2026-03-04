using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _211system.Models.Hospital
{
    public class Ambulance
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public AmbulanceType Type { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; }

        [Required]
        public Guid HospitalId { get; set; }

        [ForeignKey(nameof(HospitalId))]
        public virtual Hospital Hospital { get; set; }

        public virtual ICollection<AmbulanceEquipment> Equipment { get; set; } = new List<AmbulanceEquipment>();
    }
}
