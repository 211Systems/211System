using _211system.Models.Dtos.Police;

namespace _211system.Models.Dtos.Police
{
    public class CreatePDepartmentDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
    }

    public class CreatePolicemanDto
    {
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string BadgeNumber { get; set; }
        public string Rank { get; set; }
        public string Email { get; set; }
        public Guid PDepartmentId { get; set; }
    }

    public class PolicemanCreatedDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string TemporaryPassword { get; set; }
    }

    public class CreatePoliceCarDto
    {
        public string LicensePlate { get; set; }
        public Guid PDepartmentId { get; set; }
        public Guid? PolicemanId { get; set; }
    }

    public class UpdatePoliceCarDto
    {
        public string LicensePlate { get; set; }
        public Guid? PolicemanId { get; set; }
    }

    public class PDepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
    }

    public class PolicemanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string BadgeNumber { get; set; }
        public string Rank { get; set; }
        public Guid PDepartmentId { get; set; }
        public string PoliceAccountId { get; set; }
    }

    public class PoliceCarDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }
        public Guid PDepartmentId { get; set; }
        public bool IsAvailable { get; set; }
        public Guid? PolicemanId { get; set; }
    }
}