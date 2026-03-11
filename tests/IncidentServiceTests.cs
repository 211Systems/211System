using Xunit;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.Services;
using _211system.DTOs.CPR112;
using CPR112.Models;

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
}