using Microsoft.AspNetCore.Identity;

namespace _211system.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
    }
}