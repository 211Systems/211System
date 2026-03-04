using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _211system.Models.Hospital
{
    public class GivenMedicine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Dose { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; }

        [Required]
        public Guid ParamedicId { get; set; }

        [ForeignKey(nameof(ParamedicId))]
        public virtual Paramedic Paramedic { get; set; }
    }
}
