using _211system.Data;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using _211system.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;


var builder = WebApplication.CreateBuilder(args);

var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<_211DbContext>(options => options.UseNpgsql(ConnectionString));
builder.Services.AddScoped<IMedicalService, MedicalService>();

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
builder.Services.AddScoped<IEncService, EncService>();
builder.Services.AddScoped<IOperatorService, OperatorService>();
builder.Services.AddScoped<IPoliceService, PoliceService>();
builder.Services.AddScoped<IFireService, FireService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddOpenApiDocument();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    // Available at: http://localhost:<port>/swagger/v1/swagger.json
    app.UseOpenApi();

    // Add web UIs to interact with the document
    // Available at: http://localhost:<port>/swagger
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
[ExcludeFromCodeCoverage]
public partial class Program { }