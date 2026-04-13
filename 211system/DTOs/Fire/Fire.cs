namespace _211system.Models.Dtos.Fire
{
    public class CreateFDepartmentDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
    }

    public class CreateFiremanDto
    {
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string BadgeNumber { get; set; }
        public string Rank { get; set; }
        public string Email { get; set; }
        public Guid FDepartmentId { get; set; }
        public string FireAccountId { get; set; }
    }

    public class FiremanCreatedDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string TemporaryPassword { get; set; }
    }

    public class CreateFireTruckDto
    {
        public string LicensePlate { get; set; }
        public Guid FDepartmentId { get; set; }
        public Guid? FiremanId { get; set; }
    }

    public class UpdateFireTruckDto
    {
        public string LicensePlate { get; set; }
        public Guid? FiremanId { get; set; }
    }

    public class FDepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
    }

    public class FiremanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string BadgeNumber { get; set; }
        public string Rank { get; set; }
        public Guid FDepartmentId { get; set; }
        public string FireAccountId { get; set; }
    }

    public class FireTruckDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }
        public Guid FDepartmentId { get; set; }
        public bool IsAvailable { get; set; }
        public Guid? FiremanId { get; set; }
    }
}