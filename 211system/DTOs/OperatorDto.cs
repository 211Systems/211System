using System;

namespace _211system.DTOs;

public class OperatorDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StationNumber { get; set; }
    
    public string OpAccountId { get; set; }
    public Guid EncId { get; set; }
}

public class CreateOperatorDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StationNumber { get; set; }
    public string OpAccountId { get; set; } 
    public Guid EncId { get; set; }
}