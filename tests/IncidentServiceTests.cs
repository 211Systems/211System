using Xunit;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.Services;
using _211system.DTOs.CPR112;
using CPR112.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace _211system.Tests;

public class IncidentServiceTests
{
    private _211DbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<_211DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var databaseContext = new _211DbContext(options);
        databaseContext.Database.EnsureCreated();
        return databaseContext;
    }

    [Fact]
    public async Task CreateIncidentAsync_ShouldAddNewIncident()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);
        var dto = new CreateIncidentDto
        {
            Description = "Pożar w kuchni",
            Severity = "Wysoki",
            LocationId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid()
        };

        var result = await service.CreateIncidentAsync(dto);

        Assert.NotNull(result);
        Assert.Contains("112/", result.IncidentNumber);
        Assert.Equal("Nowe", result.Status);
        Assert.Equal(1, await context.Incidents.CountAsync());
    }

    [Fact]
    public async Task GetIncidentByIdAsync_ShouldReturnIncident_WhenExists()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);
        var incidentId = Guid.NewGuid();
        context.Incidents.Add(new Incident { 
            Id = incidentId, 
            IncidentNumber = "TEST/1", 
            Description = "Test", 
            Status = "Nowe", 
            Severity = "Niski", 
            ReportDate = DateTime.UtcNow,
            LocationId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var result = await service.GetIncidentByIdAsync(incidentId);

        Assert.NotNull(result);
        Assert.Equal(incidentId, result.Id);
    }

    [Fact]
    public async Task GetIncidentByIdAsync_ShouldThrowException_WhenNotExists()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetIncidentByIdAsync(Guid.NewGuid()));
        Assert.Equal("Nie znaleziono zgłoszenia.", exception.Message);
    }

    [Fact]
    public async Task ChangeIncidentStatusAsync_ShouldUpdateStatusAndSaveOperatorInHistory()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);
        
        var incidentId = Guid.NewGuid();
        var trustedOperatorId = Guid.NewGuid();

        context.Incidents.Add(new Incident
        {
            Id = incidentId,
            IncidentNumber = "112/2026/03/18/001",
            Status = "Nowe",
            Description = "Wypadek",
            Severity = "Wysoki",
            LocationId = Guid.NewGuid(),
            ReportDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var dto = new ChangeIncidentStatusDto { NewStatus = "Zakończone", NewSeverity = "Wysoki" };

        await service.ChangeIncidentStatusAsync(incidentId, trustedOperatorId, dto);

        var updatedIncident = await context.Incidents.FindAsync(incidentId);
        var historyEntry = await context.StatusHistories.FirstOrDefaultAsync(h => h.IncidentId == incidentId);

        Assert.Equal("Zakończone", updatedIncident.Status);
        Assert.NotNull(historyEntry);
        Assert.Equal("Nowe", historyEntry.OldStatus);
        Assert.Equal("Zakończone", historyEntry.NewStatus);
        Assert.Equal(trustedOperatorId, historyEntry.OperatorId);
    }

    [Fact]
    public async Task ChangeIncidentStatusAsync_ShouldThrowException_WhenIncidentNotFound()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ChangeIncidentStatusAsync(Guid.NewGuid(), Guid.NewGuid(), new ChangeIncidentStatusDto { NewStatus = "W toku" }));
    }

    [Fact]
    public async Task ChangeIncidentStatusAsync_ShouldThrowException_WhenStatusIsSame()
    {
        var context = GetDatabaseContext();
        var service = new IncidentService(context);
        var incidentId = Guid.NewGuid();

        context.Incidents.Add(new Incident { 
            Id = incidentId, 
            Status = "Nowe", 
            IncidentNumber = "1", 
            Description = "T", 
            Severity = "W", 
            LocationId = Guid.NewGuid(),
            ReportDate = DateTime.UtcNow 
        });
        await context.SaveChangesAsync();

        var dto = new ChangeIncidentStatusDto { NewStatus = "Nowe" };
        
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.ChangeIncidentStatusAsync(incidentId, Guid.NewGuid(), dto));
        
        Assert.Equal("Zgłoszenie posiada już ten status.", exception.Message);
    }
}