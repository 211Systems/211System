namespace _211system.DTOs;

public class EncDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Region{ get; set; }
}

public class CreateEncDto
{
    public string Name { get; set; }
    public string Region{ get; set; }
}