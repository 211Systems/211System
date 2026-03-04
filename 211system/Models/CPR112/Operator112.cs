using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CPR112.Models;
public class Operator112 {
    [Key]
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StationNumber { get; set; }
    public IdentityUser IdentityUserId { get; set; } 
    public Guid ENCId { get; set; }
    public ENC Center { get; set; }
}