using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CPR112.Models;

namespace _211system.Models.Aviation
{
    public class AviationOperation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AirUnitId { get; set; }
        [ForeignKey("AirUnitId")]
        public AirUnit AirUnit { get; set; }

        public Guid? IncidentId { get; set; }
        [ForeignKey("IncidentId")]
        public Incident Incident { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}