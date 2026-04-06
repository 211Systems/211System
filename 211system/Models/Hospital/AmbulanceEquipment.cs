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
        public string Name { get; set; }

        [Required]
        public Guid AmbulanceId { get; set; }

        public int Quantity { get; set; }

        [ForeignKey(nameof(AmbulanceId))]
        public virtual Ambulance Ambulance { get; set; }
    }
}
