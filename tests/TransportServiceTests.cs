using _211system.Data;
using _211system.DTOs;
using _211system.Services;
using Microsoft.EntityFrameworkCore;

namespace tests;

public class TransportServiceTests
{
    private _211DbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<_211DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new _211DbContext(options);
    }

    [Fact]
    public async Task RecordAsync_Should_Persist_TransportRecord()
    {
        var context = GetContext();
        var service = new TransportService(context);

        var incidentId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        await service.RecordAsync(new RecordTransportDto
        {
            IncidentId = incidentId,
            VehicleId = vehicleId,
            VehicleType = "Medic",
            VehicleLabel = "WA 11111",
            DestinationId = destId,
            DestinationName = "Szpital Test",
            DestinationType = "hospital"
        });

        var record = await context.TransportRecords.SingleAsync();
        Assert.Equal(incidentId, record.IncidentId);
        Assert.Equal(vehicleId, record.VehicleId);
        Assert.Equal("medic", record.VehicleType);
        Assert.Equal("WA 11111", record.VehicleLabel);
        Assert.Equal("Szpital Test", record.DestinationName);
        Assert.Equal("hospital", record.DestinationType);
    }

    [Fact]
    public async Task RecordAsync_WithEmptyIncidentId_ShouldThrow()
    {
        var service = new TransportService(GetContext());

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordAsync(new RecordTransportDto
        {
            IncidentId = Guid.Empty,
            VehicleId = Guid.NewGuid(),
            DestinationId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task RecordAsync_MultipleTransports_PerIncident()
    {
        var context = GetContext();
        var service = new TransportService(context);
        var incidentId = Guid.NewGuid();

        await service.RecordAsync(new RecordTransportDto
        {
            IncidentId = incidentId,
            VehicleId = Guid.NewGuid(),
            VehicleType = "medic",
            VehicleLabel = "K1",
            DestinationId = Guid.NewGuid(),
            DestinationName = "Szpital A",
            DestinationType = "hospital"
        });
        await service.RecordAsync(new RecordTransportDto
        {
            IncidentId = incidentId,
            VehicleId = Guid.NewGuid(),
            VehicleType = "police",
            VehicleLabel = "P1",
            DestinationId = Guid.NewGuid(),
            DestinationName = "Komenda B",
            DestinationType = "police_station"
        });

        var count = await context.TransportRecords.CountAsync(r => r.IncidentId == incidentId);
        Assert.Equal(2, count);
    }
}
