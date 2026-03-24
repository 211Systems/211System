namespace _211system.DTOs;

public class StartPoliceOperationDto
{
    public Guid PDepartmentId { get; set; }
    public Guid IncidentId { get; set; }
}

public class StartFireOperationDto
{
    public Guid FDepartmentId { get; set; }
    public Guid IncidentId { get; set; }
}