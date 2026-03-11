using System;

namespace _211system.DTOs;

public class ReadinessBoardDto
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentIncidentId { get; set; } 
}