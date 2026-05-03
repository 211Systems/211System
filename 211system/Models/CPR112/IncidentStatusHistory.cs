using System;
using CPR112.Models;

namespace _211system.Models
{
    public class IncidentStatusHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IncidentId { get; set; }
        public Incident Incident { get; set; }
        public Guid? OperatorId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}