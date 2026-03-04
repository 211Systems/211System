using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CPR112.Models;
public class Operator112 {
    [Key]
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StationNumber { get; set; }
    public string OpAccountId { get; set; }
    [ForeignKey(nameof(OpAccountId))]
    public virtual IdentityUser OpAccount { get; set; }
    public Guid EncId { get; set; }
    public Enc Center { get; set; }
}