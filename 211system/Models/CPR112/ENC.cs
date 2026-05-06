using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class Enc {
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Region { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double OperatingRadiusKm { get; set; }
    public virtual ICollection<Operator112> Operators { get; set; } = new List<Operator112>();
}