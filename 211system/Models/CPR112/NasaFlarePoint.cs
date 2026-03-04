using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class NasaFlarePoint 
{
    [Key]
    public Guid Id { get; set; }
    public double Brightness { get; set; }
    public DateTime DetectionDate { get; set; }
}