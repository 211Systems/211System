using System.ComponentModel.DataAnnotations;

namespace _211system.Models.Hospital
{
    public class Hospital
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public bool HasSOR { get; set; } = false;
        [Required]
        [MaxLength(150)]
        public string Address { get; set; }

        public virtual ICollection<Paramedic> Paramedics { get; set; } = new List<Paramedic>();
        public virtual ICollection<HospitalWard> Wards { get; set; } = new List<HospitalWard>();
        public virtual ICollection<Ambulance> Ambulances { get; set; } = new List<Ambulance>();

    }
}
