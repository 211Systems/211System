using System.ComponentModel.DataAnnotations;

public class Location {
    [Key]
    public Guid Id { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}