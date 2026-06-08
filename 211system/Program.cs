using System.Diagnostics.CodeAnalysis;
using System.Text;
using _211system.Configuration;
using _211system.Data;
using _211system.Models;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using _211system.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

AppConfiguration.ValidateRequired(builder.Configuration);

var ConnectionString = builder.Configuration.GetConnectionString(AppConfiguration.DefaultConnectionName);
builder.Services.AddDbContext<_211DbContext>(options => options.UseNpgsql(ConnectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<_211DbContext>()
.AddDefaultTokenProviders();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
builder.Services.AddScoped<IIdentitySeedService, IdentitySeedService>();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("211Cors", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

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
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IAviationService, AviationService>();
builder.Services.AddScoped<ITransportService, TransportService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddHttpClient<IAiService, GeminiAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddControllersWithViews();

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

using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var runMigrations = configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment());

    if (runMigrations)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<_211DbContext>();
        await dbContext.Database.MigrateAsync();
    }

    var identitySeed = scope.ServiceProvider.GetRequiredService<IIdentitySeedService>();
    await identitySeed.SeedAsync();
}

app.UseForwardedHeaders();

if (corsOrigins.Length > 0)
{
    app.UseCors("211Cors");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
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
