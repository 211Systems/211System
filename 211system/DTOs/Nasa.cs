namespace _211system.Models.Dtos.Nasa
{
    public class NasaFetchResultDto
    {
        public int TotalAnomaliesDetected { get; set; }
        public int IncidentsGenerated { get; set; }
        public List<NasaIncidentDto> GeneratedIncidents { get; set; } = new();
    }

    public class NasaIncidentDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Brightness { get; set; }
        public string Description { get; set; }
    }
}
    