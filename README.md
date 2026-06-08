# 211System

## Konfiguracja (appsettings)

Kolejność ładowania w ASP.NET Core:

1. `appsettings.json` — wspólna struktura, **bez sekretów produkcyjnych**
2. `appsettings.{Environment}.json` — nadpisania (Development / Production)
3. User Secrets — tylko lokalnie w Development
4. Zmienne środowiskowe / Azure App Settings — **najwyższy priorytet**

### Lokalnie (Development)

`launchSettings.json` ustawia `ASPNETCORE_ENVIRONMENT=Development`.

- Baza i Azurite: `appsettings.Development.json`
- `Jwt:Key` — User Secrets:

```bash
cd 211system
dotnet user-secrets set "Jwt:Key" "twoj-lokalny-klucz-min-32-znakow"
```

### Azure App Service (Production)

W portalu Azure → App Service → **Configuration**:

**Application settings**

| Azure (Name) | Odpowiednik JSON | Opis |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | — | `Production` |
| `Jwt__Key` | `Jwt:Key` | Klucz JWT (min. 32 znaki) |
| `SeedAdmins__Admin__Password` | `SeedAdmins:Admin:Password` | Hasło admina (pierwszy start) |
| `SeedAdmins__Admin112__Password` | `SeedAdmins:Admin112:Password` | Hasło admin112 |
| `SeedAdmins__Enabled` | `SeedAdmins:Enabled` | `false` po utworzeniu kont |
| `NasaApiKey` | `NasaApiKey` | Opcjonalnie — NASA FIRMS |
| `GeminiApiKey` | `GeminiApiKey` | Opcjonalnie — AI |
| `OpenAI__ApiKey` | `OpenAI:ApiKey` | Opcjonalnie |
| `WeatherApis__OpenWeatherMapKey` | `WeatherApis:OpenWeatherMapKey` | Opcjonalnie |
| `WeatherApis__AvwxKey` | `WeatherApis:AvwxKey` | Opcjonalnie |

**Connection strings** (typ: PostgreSQL / Custom)

| Azure (Name) | Opis |
|---|---|
| `DefaultConnection` | Connection string do Azure Database for PostgreSQL |
| `AzureBlobStorage` | Connection string do konta Storage (Blob) |

Alternatywnie jako Application settings:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__AzureBlobStorage`

CORS i domeny: `appsettings.Production.json` (AllowedHosts, Cors:AllowedOrigins) lub nadpisanie przez `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, …

### Migracje bazy (Production)

Przy starcie aplikacji (App Service w VNet) migracje EF uruchamiają się automatycznie, gdy `Database:AutoMigrate` = `true`.

- Domyślnie włączone w `appsettings.Production.json`
- **Po pierwszym udanym wdrożeniu** ustaw w Azure App settings: `Database__AutoMigrate` = `false`

Lokalnie (Development) migracje przy starcie działają bez dodatkowej konfiguracji.

Ręcznie (opcjonalnie, np. z jumpboxa w VNet):

```bash
dotnet ef database update --project 211system
```
