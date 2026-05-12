namespace _211system.Models.Dtos
{
    public class GroundWeatherDto
    {
        public double Temperature { get; set; }
        public string Description { get; set; }
        public int VisibilityMeters { get; set; }
        public string IconUrl { get; set; }

        public bool IsSlippery { get; set; }
        public bool IsFoggy { get; set; }
        public bool IsStormy { get; set; }
    }

    public class FlightWeatherDto
    {
        public string StationIcao { get; set; }
        public string FlightRules { get; set; }
        public string RawMetar { get; set; }
        public bool IsFlightRecommended => FlightRules == "VFR" || FlightRules == "MVFR";
    }
}