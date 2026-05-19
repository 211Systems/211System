using System;
using System.Collections.Generic;

namespace _211system.DTOs.Ai
{
    public class AiDispatchRequestDto
    {
        public List<AiIncidentDto> Incidents { get; set; } = new();
        public List<AiUnitDto> AvailableAmbulances { get; set; } = new();
        public List<AiUnitDto> AvailableFireTrucks { get; set; } = new();
        public List<AiUnitDto> AvailablePoliceCars { get; set; } = new();
    }

    public class AiIncidentDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string IncidentType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AiUnitDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AiDispatchResponseDto
    {
        public List<AiDispatchSuggestion> Suggestions { get; set; } = new();
    }

    public class AiDispatchSuggestion
    {
        public Guid IncidentId { get; set; }
        public Guid UnitId { get; set; }
        public string UnitType { get; set; }
        public string Reasoning { get; set; }
        public string IncidentDescription { get; set; }
        public string UnitName { get; set; }
    }
}