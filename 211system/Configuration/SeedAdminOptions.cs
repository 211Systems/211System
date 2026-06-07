namespace _211system.Configuration;

public class SeedAdminOptions
{
    public const string SectionName = "SeedAdmins";

    public bool Enabled { get; set; } = true;

    public SeedAdminAccountOptions Admin { get; set; } = new();

    public SeedAdminAccountOptions Admin112 { get; set; } = new();
}

public class SeedAdminAccountOptions
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
