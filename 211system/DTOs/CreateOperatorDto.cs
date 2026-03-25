using System;

namespace _211system.DTOs.CPR112
{
    public class CreateOperatorDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string StationNumber { get; set; } = string.Empty;
        public Guid EncId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rank { get; set; } = "Dyspozytor112"; 
    }
}