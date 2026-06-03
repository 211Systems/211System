using System;
using System.Linq;
using System.Threading.Tasks;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Models;
using _211system.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace _211system.Tests
{
    public class IncidentServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new _211DbContext(options);

            if (!context.SeverityLevels.Any())
            {
                context.SeverityLevels.AddRange(
                    new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" },
                    new SeverityLevel { Id = 2, Name = "Średni", ColorCode = "warning" },
                    new SeverityLevel { Id = 3, Name = "Wysoki", ColorCode = "danger" }
                );
                context.IncidentTypes.AddRange(
                    new IncidentType { Id = 1, Name = "Wypadek drogowy", RequiresPolice = true, RequiresMedic = true, RequiresFire = true }
                );
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task CreateIncidentAsync_ShouldCreateIncidentAndReturnDto()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 3,
                IncidentTypeId = 1,
                Latitude = "52.2297",
                Longitude = "21.0122"
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
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.2297,
                Longitude = 21.0122
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
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.2297,
                Longitude = 21.0122
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto
            {
                NewStatus = "W toku",
                NewSeverityLevelId = 3
            };

            await service.ChangeIncidentStatusAsync(incidentId, operatorId, dto);

            var updatedIncident = await context.Incidents.FindAsync(incidentId);
            Assert.Equal("W toku", updatedIncident.Status);
            Assert.Equal(3, updatedIncident.SeverityLevelId);
            Assert.True(context.IncidentStatusHistories.Any(h => h.IncidentId == incidentId && h.NewStatus == "W toku"));
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
                SeverityLevelId = 3,
                IncidentTypeId = 1,
                IncidentNumber = "123",
                Description = "Test",
                Latitude = 52.2297,
                Longitude = 21.0122
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto
            {
                NewStatus = "Nowe",
                NewSeverityLevelId = 3,
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
                SeverityLevelId = 3,
                IncidentTypeId = 1,
                IncidentNumber = "123",
                Description = "Test",
                Latitude = 52.2297,
                Longitude = 21.0122
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();

            var dto = new ChangeIncidentStatusDto
            {
                NewStatus = "Nowe",
                NewSeverityLevelId = 3,
                NewPhotoUrl = "http://azure.com/newphoto.jpg"
            };

            var exception = await Record.ExceptionAsync(() =>
                service.ChangeIncidentStatusAsync(incidentId, Guid.NewGuid(), dto));

            Assert.Null(exception);
            var updated = await context.Incidents.FindAsync(incidentId);
            Assert.Equal("http://azure.com/newphoto.jpg", updated.PhotoUrl);
        }

        [Fact]
        public async Task CreateIncidentAsync_InvalidIncidentType_Throws()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 1,
                IncidentTypeId = 999,
                Latitude = "52.0",
                Longitude = "21.0"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateIncidentAsync(dto));
        }

        [Fact]
        public async Task GetIncidentByIdAsync_NotFound_Throws()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetIncidentByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ChangeIncidentStatusAsync_WritesStatusHistory()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            context.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/1",
                Description = "Historia",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.ChangeIncidentStatusAsync(incidentId, operatorId, new ChangeIncidentStatusDto
            {
                NewStatus = "W toku",
                NewSeverityLevelId = 2
            });

            var history = await context.IncidentStatusHistories.FirstAsync(h => h.IncidentId == incidentId);
            Assert.Equal("Nowe", history.OldStatus);
            Assert.Equal("W toku", history.NewStatus);
            Assert.Equal(operatorId, history.OperatorId);
        }

        [Fact]
        public async Task ChangeIncidentStatusAsync_ToClosed_UpdatesIncident()
        {
            var context = GetInMemoryDbContext();
            var service = new IncidentService(context);
            var incidentId = Guid.NewGuid();

            context.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/2",
                Description = "Zamkniecie",
                Status = "W toku",
                SeverityLevelId = 2,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.ChangeIncidentStatusAsync(incidentId, Guid.NewGuid(), new ChangeIncidentStatusDto
            {
                NewStatus = "Zakończone",
                NewSeverityLevelId = 1
            });

            var updated = await context.Incidents.FindAsync(incidentId);
            Assert.Equal("Zakończone", updated.Status);
            Assert.Equal(1, updated.SeverityLevelId);
        }
    }
}