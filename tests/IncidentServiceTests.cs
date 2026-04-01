using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Services;
using CPR112.Models;

namespace _211system.Tests
{
    public class IncidentServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        [Fact]
        public async Task CreateIncidentAsync_ShouldCreateIncidentAndReturnDto()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var dto = new CreateIncidentDto
            {
                Description = "Test Incident",
                Severity = "Wysoki",
                LocationId = Guid.NewGuid()
            };

            var result = await service.CreateIncidentAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal("Nowe", result.Status);
            Assert.True(context.Incidents.Any());
        }

        [Fact]
        public async Task GetIncidentByIdAsync_ShouldReturnCorrectIncident()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();
            var incident = new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/2024/01/01/001",
                Description = "Test",
                Status = "Nowe",
                Severity = "Niski",
                ReportDate = DateTime.UtcNow,
                LocationId = Guid.NewGuid()
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var result = await service.GetIncidentByIdAsync(incidentId);

            Assert.NotNull(result);
            Assert.Equal(incidentId, result.Id);
        }

        [Fact]
        public async Task ChangeIncidentStatusAsync_ShouldUpdateStatusAndLogHistory()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            
            var incident = new Incident
            {
                Id = incidentId,
                IncidentNumber = "123",
                Description = "Test",
                Status = "Nowe",
                Severity = "Niski",
                ReportDate = DateTime.UtcNow,
                LocationId = Guid.NewGuid()
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto
            {
                NewStatus = "W toku",
                NewSeverity = "Wysoki"
            };

            await service.ChangeIncidentStatusAsync(incidentId, operatorId, dto);

            var updatedIncident = await context.Incidents.FindAsync(incidentId);
            Assert.Equal("W toku", updatedIncident.Status);
            Assert.Equal("Wysoki", updatedIncident.Severity);
            Assert.True(context.StatusHistories.Any(h => h.IncidentId == incidentId && h.NewStatus == "W toku"));
        }

        [Fact]
        public async Task ChangeIncidentStatusAsync_ShouldThrowException_WhenStatusIsSame()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();
            
            var incident = new Incident 
            { 
                Id = incidentId, 
                Status = "Nowe", 
                Severity = "Wysoki",
                IncidentNumber = "123",
                Description = "Test",
                LocationId = Guid.NewGuid()
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto 
            { 
                NewStatus = "Nowe", 
                NewSeverity = "Wysoki",
                NewPhotoUrl = null
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.ChangeIncidentStatusAsync(incidentId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task ChangeIncidentStatusAsync_ShouldNotThrow_WhenPhotoIsAddedEvenIfStatusIsSame()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();
            
            var incident = new Incident 
            { 
                Id = incidentId, 
                Status = "Nowe", 
                Severity = "Wysoki",
                IncidentNumber = "123",
                Description = "Test",
                LocationId = Guid.NewGuid()
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto 
            { 
                NewStatus = "Nowe", 
                NewSeverity = "Wysoki",
                NewPhotoUrl = "http://azure.com/newphoto.jpg"
            };

            var exception = await Record.ExceptionAsync(() => 
                service.ChangeIncidentStatusAsync(incidentId, Guid.NewGuid(), dto));
            
            Assert.Null(exception);
            var updated = await context.Incidents.FindAsync(incidentId);
            Assert.Equal("http://azure.com/newphoto.jpg", updated.PhotoUrl);
        }
    }
}