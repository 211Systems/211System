using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CPR112.Models;

namespace _211system.Models.Hospital
{
    public class MedicalOperation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReportId { get; set; }

        [Required]
        public Guid ParamedicId { get; set; }

        [ForeignKey(nameof(ParamedicId))]
        public virtual Paramedic Paramedic { get; set; }

        //[ForeignKey(nameof(IncidentId))]
        public virtual Incident Incident { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }


    }
}
