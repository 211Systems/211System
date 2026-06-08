namespace _211system.Configuration;

/// <summary>
/// Nazwy kluczy konfiguracji — używaj tych samych w appsettings, User Secrets i Azure App Settings.
/// W Azure podwójny underscore __ odpowiada zagnieżdżeniu JSON (np. Jwt__Key → Jwt:Key).
/// </summary>
public static class AppConfiguration
{
    public const string DefaultConnectionName = "DefaultConnection";
    public const string AzureBlobStorageConnectionName = "AzureBlobStorage";

    public static void ValidateRequired(IConfiguration configuration)
    {
        RequireConnectionString(configuration, DefaultConnectionName,
            "PostgreSQL — lokalnie w appsettings.Development.json / User Secrets; w Azure: Connection string „DefaultConnection” lub zmienna ConnectionStrings__DefaultConnection.");

        RequireConnectionString(configuration, AzureBlobStorageConnectionName,
            "Azure Blob Storage — lokalnie Azurite w appsettings.Development.json; w Azure: Connection string „AzureBlobStorage” lub ConnectionStrings__AzureBlobStorage.");

        RequireSetting(configuration, "Jwt:Key",
            "User Secrets (dev): dotnet user-secrets set \"Jwt:Key\" \"...\"; w Azure: Jwt__Key.");
    }

    private static void RequireConnectionString(IConfiguration configuration, string name, string hint)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(name)))
        {
            throw new InvalidOperationException(
                $"Brak ConnectionStrings:{name}. {hint}");
        }
    }

    private static void RequireSetting(IConfiguration configuration, string key, string hint)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            throw new InvalidOperationException($"Brak {key}. {hint}");
        }
    }
}
