using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class PeriodicReport {
    [Key]
    public Guid Id { get; set; }
    public string PathToPDF { get; set; }
    public DateTime GenerationDate { get; set; }
}