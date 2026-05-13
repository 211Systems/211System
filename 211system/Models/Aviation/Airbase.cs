using System;
using System.Collections.Generic;

namespace _211system.Models.Aviation
{
    public class Airbase
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string IcaoCode { get; set; }
        public ServiceType ServiceType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public ICollection<AirUnit> AirUnits { get; set; } = new List<AirUnit>();
    }
}