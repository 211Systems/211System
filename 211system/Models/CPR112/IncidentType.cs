using System.Collections.Generic;
using CPR112.Models;

namespace _211system.Models
{
    public class IncidentType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public bool RequiresPolice { get; set; }
        public bool RequiresFire { get; set; }
        public bool RequiresMedic { get; set; }
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }
}