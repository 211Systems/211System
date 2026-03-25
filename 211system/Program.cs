using System.Diagnostics.CodeAnalysis;
using System.Text;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using _211system.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CPR112.Models; // Upewnij się, że ten namespace pasuje do lokalizacji Twojego Operator112

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// BAZA DANYCH
var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<_211DbContext>(options => options.UseNpgsql(ConnectionString));

// IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<_211DbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// REJESTRACJA SERWISÓW
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMedicalService, MedicalService>();
builder.Services.AddScoped<IEncService, EncService>();
builder.Services.AddScoped<IOperatorService, OperatorService>();
builder.Services.AddScoped<IPoliceService, PoliceService>();
builder.Services.AddScoped<IFireService, FireService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<IReadinessService, ReadinessService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<INasaService, NasaService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

builder.Services.AddControllersWithViews();

// SWAGGER / OPENAPI
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "211 System API";
    config.AddSecurity("Bearer", Enumerable.Empty<string>(), new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header
    });
    config.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

// SEED DANYCH (ROLE I ADMINI)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<_211DbContext>();

    // 1. Tworzenie ról
    string[] roleNames = { "Admin", "Admin112", "Dyspozytor112", "Inspektor", "Komendant", "Policjant",
        "Naczelnik", "Strazak", "Kapitan", "Medyk", "Lekarz", "Kierownik Szpitala" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // 2. Tworzenie Globalnego Admina
    string adminEmail = "admin@211.pl";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // 3. Tworzenie Admina 112 i powiązanie z tabelą Operator112
    string admin112Email = "admin112@211.pl";
    if (await userManager.FindByEmailAsync(admin112Email) == null)
    {
        var admin112User = new IdentityUser { UserName = admin112Email, Email = admin112Email };
        var result = await userManager.CreateAsync(admin112User, "Admin112!");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin112User, "Admin112");

            // Szukamy centrum (Enc) do przypisania
            var center = await dbContext.Encs.FirstOrDefaultAsync();
            if (center != null)
            {
                dbContext.Operators112.Add(new Operator112
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Główny",
                    LastName = "Administrator",
                    StationNumber = "ADM-01",
                    OpAccountId = admin112User.Id,
                    Rank = OperatorRank.Admin112,
                    EncId = center.Id
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Poprawione z MapStaticAssets dla lepszej kompatybilności
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }