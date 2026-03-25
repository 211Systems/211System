using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using _211system.Models;

namespace CPR112.Models
{
    public enum OperatorRank
    {
        Dyspozytor112,
        Admin112
    }

    public class Operator112 
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string StationNumber { get; set; } = string.Empty;

        [Required]
        public OperatorRank Rank { get; set; }

        [Required]
        public string OpAccountId { get; set; } = string.Empty;

        [ForeignKey(nameof(OpAccountId))]
        public virtual IdentityUser? OpAccount { get; set; }

        [Required]
        public Guid EncId { get; set; }

        [ForeignKey(nameof(EncId))]
        public virtual Enc? Center { get; set; }
    }
}