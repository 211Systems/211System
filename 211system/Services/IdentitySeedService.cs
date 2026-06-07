using _211system.Configuration;
using _211system.Data;
using _211system.Models;
using CPR112.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace _211system.Services;

public interface IIdentitySeedService
{
    Task SeedAsync();
}

public class IdentitySeedService : IIdentitySeedService
{
    private static readonly string[] RoleNames =
    {
        "Admin", "Admin112", "Dyspozytor112", "Inspektor", "Komendant", "Policjant",
        "Naczelnik", "Strazak", "Kapitan", "Medyk", "Lekarz", "Kierownik Szpitala"
    };

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly _211DbContext _dbContext;
    private readonly SeedAdminOptions _options;
    private readonly ILogger<IdentitySeedService> _logger;

    public IdentitySeedService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        _211DbContext dbContext,
        IOptions<SeedAdminOptions> options,
        ILogger<IdentitySeedService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();

        if (!_options.Enabled)
        {
            _logger.LogInformation("Seed administratorów wyłączony (SeedAdmins:Enabled=false).");
            return;
        }

        await SeedAdminAsync(_options.Admin, "Admin");
        await SeedAdmin112Async(_options.Admin112);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in RoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private async Task SeedAdminAsync(SeedAdminAccountOptions account, string role)
    {
        if (string.IsNullOrWhiteSpace(account.Email))
        {
            _logger.LogWarning("Pominięto seed konta {Role} – brak e-maila w konfiguracji.", role);
            return;
        }

        if (string.IsNullOrWhiteSpace(account.Password))
        {
            _logger.LogWarning(
                "Pominięto seed konta {Email} – ustaw hasło (SeedAdmins:Admin:Password).",
                account.Email);
            return;
        }

        if (await _userManager.FindByEmailAsync(account.Email) != null)
            return;

        var user = new ApplicationUser { UserName = account.Email, Email = account.Email };
        var result = await _userManager.CreateAsync(user, account.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            _logger.LogInformation("Utworzono konto seed: {Email} ({Role}).", account.Email, role);
        }
        else
        {
            _logger.LogError(
                "Nie udało się utworzyć konta seed {Email}: {Errors}",
                account.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedAdmin112Async(SeedAdminAccountOptions account)
    {
        if (string.IsNullOrWhiteSpace(account.Email))
        {
            _logger.LogWarning("Pominięto seed Admin112 – brak e-maila w konfiguracji.");
            return;
        }

        if (string.IsNullOrWhiteSpace(account.Password))
        {
            _logger.LogWarning(
                "Pominięto seed konta {Email} – ustaw hasło (SeedAdmins:Admin112:Password).",
                account.Email);
            return;
        }

        if (await _userManager.FindByEmailAsync(account.Email) != null)
            return;

        var user = new ApplicationUser { UserName = account.Email, Email = account.Email };
        var result = await _userManager.CreateAsync(user, account.Password);

        if (!result.Succeeded)
        {
            _logger.LogError(
                "Nie udało się utworzyć konta seed {Email}: {Errors}",
                account.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(user, "Admin112");

        var center = await _dbContext.Encs.FirstOrDefaultAsync();
        if (center != null)
        {
            _dbContext.Operators112.Add(new Operator112
            {
                Id = Guid.NewGuid(),
                FirstName = "Główny",
                LastName = "Administrator",
                StationNumber = "ADM-01",
                OpAccountId = user.Id,
                Rank = OperatorRank.Admin112,
                EncId = center.Id
            });
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Utworzono konto seed: {Email} (Admin112).", account.Email);
    }
}
