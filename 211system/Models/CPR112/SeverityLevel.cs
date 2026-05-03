using System.Collections.Generic;
using CPR112.Models;

namespace _211system.Models
{
    public class SeverityLevel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ColorCode { get; set; } 

        // Relacja 1:N z Incident
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }
}