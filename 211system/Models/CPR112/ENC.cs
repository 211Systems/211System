using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class ENC {
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Region { get; set; }
}