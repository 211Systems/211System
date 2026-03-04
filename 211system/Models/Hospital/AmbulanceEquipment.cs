using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _211system.Models.Hospital
{
    public class AmbulanceEquipment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; }

        [Required]
        public Guid AmbulanceId { get; set; }

        [ForeignKey(nameof(AmbulanceId))]
        public virtual Ambulance Ambulance { get; set; }
    }
}
