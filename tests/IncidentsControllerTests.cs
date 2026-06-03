using _211system.Controllers;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Models;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using _211system.Services;
using CPR112.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace _211system.Tests
{
    public class IncidentsControllerTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        private void SeedBaseData(_211DbContext context)
        {
            if (!context.SeverityLevels.Any())
            {
                context.SeverityLevels.AddRange(
                    new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" },
                    new SeverityLevel { Id = 2, Name = "Średni", ColorCode = "warning" },
                    new SeverityLevel { Id = 3, Name = "Wysoki", ColorCode = "danger" }
                );
            }

            if (!context.IncidentTypes.Any())
            {
                context.IncidentTypes.AddRange(
                    new IncidentType { Id = 1, Name = "Wypadek", RequiresPolice = true, RequiresMedic = true, RequiresFire = false },
                    new IncidentType { Id = 2, Name = "Pożar", RequiresPolice = false, RequiresMedic = false, RequiresFire = true }
                );
            }

            context.SaveChanges();
        }

        private IncidentsController CreateController(
            _211DbContext context,
            Mock<IIncidentService>? serviceMock = null,
            Mock<IBlobStorageService>? blobMock = null,
            Mock<IWeatherService>? weatherMock = null)
        {
            serviceMock ??= new Mock<IIncidentService>();
            blobMock ??= new Mock<IBlobStorageService>();
            weatherMock ??= new Mock<IWeatherService>();

            blobMock
                .Setup(b => b.GetSecureFileUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns((string url, string container, int minutes) => url + "?token=test");

            return new IncidentsController(
                serviceMock.Object,
                context,
                blobMock.Object,
                weatherMock.Object);
        }

        private void SetupControllerUser(IncidentsController controller, string identityId, string role = "Dyspozytor112")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, identityId),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void SetupChangeStatusSuccess(Mock<IIncidentService> serviceMock)
        {
            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ChangeIncidentStatusDto>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task CreateIncident_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);

            SetupControllerUser(controller, "test-user-id");

            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 3,
                IncidentTypeId = 1
            };

            serviceMock
                .Setup(s => s.CreateIncidentAsync(It.IsAny<CreateIncidentDto>()))
                .ReturnsAsync(new IncidentDto { Id = Guid.NewGuid(), Description = "Test" });

            var result = await controller.CreateIncident(dto, null);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetIncidentById_Success_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);
            var id = Guid.NewGuid();

            serviceMock
                .Setup(s => s.GetIncidentByIdAsync(id))
                .ReturnsAsync(new IncidentDto { Id = id });

            var result = await controller.GetIncidentById(id);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetIncidentById_NotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);
            var id = Guid.NewGuid();

            serviceMock
                .Setup(s => s.GetIncidentByIdAsync(id))
                .ThrowsAsync(new ArgumentException("Not found"));

            var result = await controller.GetIncidentById(id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task ChangeStatus_Unauthorized_WhenNoIdentityIdInToken()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto(), null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_WhenOperatorNotInDbButAuthenticated()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);

            SetupControllerUser(controller, "admin-identity-id", "Admin112");
            SetupChangeStatusSuccess(serviceMock);

            var result = await controller.ChangeStatus(
                Guid.NewGuid(),
                new ChangeIncidentStatusDto { NewStatus = "W toku" },
                null);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_ServiceNotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                Rank = OperatorRank.Dyspozytor112,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto { NewStatus = "W toku" };

            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    incidentId,
                    operatorId,
                    It.IsAny<ChangeIncidentStatusDto>()))
                .ThrowsAsync(new ArgumentException("Incident not found"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_InvalidOperation_ShouldReturn400()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                Rank = OperatorRank.Dyspozytor112,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto { NewStatus = "W toku" };

            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    incidentId,
                    operatorId,
                    It.IsAny<ChangeIncidentStatusDto>()))
                .ThrowsAsync(new InvalidOperationException("Same status"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_ShouldReturn204()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                Rank = OperatorRank.Dyspozytor112,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);
            SetupChangeStatusSuccess(serviceMock);

            var result = await controller.ChangeStatus(
                Guid.NewGuid(),
                new ChangeIncidentStatusDto { NewStatus = "W toku" },
                null);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetAllIncidents_ReturnsOkList()
        {
            var context = GetInMemoryDbContext();
            SeedBaseData(context);

            context.Incidents.AddRange(
                new Incident
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "112/1",
                    Description = "A",
                    Status = "Nowe",
                    SeverityLevelId = 1,
                    IncidentTypeId = 1,
                    ReportDate = DateTime.UtcNow.AddHours(-1),
                    Latitude = 52.0,
                    Longitude = 21.0
                },
                new Incident
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "112/2",
                    Description = "B",
                    Status = "W toku",
                    SeverityLevelId = 2,
                    IncidentTypeId = 2,
                    ReportDate = DateTime.UtcNow,
                    Latitude = 50.0,
                    Longitude = 19.0
                }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            SetupControllerUser(controller, "user-1");

            var result = await controller.GetAllIncidents();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<IncidentDto>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task GetIncidentTypes_ReturnsSeededTypes()
        {
            var context = GetInMemoryDbContext();
            SeedBaseData(context);

            var controller = CreateController(context);
            SetupControllerUser(controller, "user-1");

            var result = await controller.GetIncidentTypes();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Equal(2, list.Cast<object>().Count());
        }

        [Fact]
        public async Task GetIncidentStats_ReturnsSummary()
        {
            var context = GetInMemoryDbContext();
            SeedBaseData(context);

            context.Incidents.AddRange(
                new Incident
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "1",
                    Description = "x",
                    Status = "Nowe",
                    SeverityLevelId = 1,
                    IncidentTypeId = 1,
                    ReportDate = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0
                },
                new Incident
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "2",
                    Description = "y",
                    Status = "Nowe",
                    SeverityLevelId = 1,
                    IncidentTypeId = 1,
                    ReportDate = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0
                },
                new Incident
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "3",
                    Description = "z",
                    Status = "Nowe",
                    SeverityLevelId = 1,
                    IncidentTypeId = 2,
                    ReportDate = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0
                }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            SetupControllerUser(controller, "user-1");

            var result = await controller.GetIncidentStats();

            var ok = Assert.IsType<OkObjectResult>(result);
            var stats = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.True(stats.Cast<object>().Any());
        }

        [Fact]
        public async Task GetIncidentHistory_ExistingId_ReturnsHistory()
        {
            var context = GetInMemoryDbContext();
            var incidentId = Guid.NewGuid();

            context.IncidentStatusHistories.Add(new IncidentStatusHistory
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                OldStatus = "Nowe",
                NewStatus = "W toku",
                ChangedAt = DateTime.UtcNow,
                OperatorId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            SetupControllerUser(controller, "hist-user");

            var result = await controller.GetIncidentHistory(incidentId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var history = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(history.Cast<object>());
        }

        [Fact]
        public async Task DeleteIncident_WithAssignedUnits_HandlesOrBlocks()
        {
            var context = GetInMemoryDbContext();
            SeedBaseData(context);

            var incidentId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();

            context.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/del",
                Description = "Do usuniecia",
                Status = "W toku",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });

            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "WA 1",
                HospitalId = Guid.NewGuid(),
                CurrentIncidentId = incidentId,
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0
            });

            context.MedicalOperations.Add(new MedicalOperation
            {
                Id = Guid.NewGuid(),
                ReportId = incidentId,
                ParamedicId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var controller = CreateController(context);
            SetupControllerUser(controller, "admin-id", "Admin");

            var result = await controller.DeleteIncident(incidentId);

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(await context.Incidents.FindAsync(incidentId));

            var ambulance = await context.Ambulances.FindAsync(ambulanceId);
            Assert.Null(ambulance.CurrentIncidentId);
            Assert.True(ambulance.IsAvailable);
            Assert.False(await context.MedicalOperations.AnyAsync(o => o.ReportId == incidentId));
        }

        [Fact]
        public async Task ChangeStatus_InvalidId_ReturnsNotFound()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-invalid";
            var operatorId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "X",
                LastName = "Y",
                StationNumber = "9",
                Rank = OperatorRank.Dyspozytor112,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);

            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    incidentId,
                    operatorId,
                    It.IsAny<ChangeIncidentStatusDto>()))
                .ThrowsAsync(new ArgumentException("Nie znaleziono zgłoszenia."));

            var result = await controller.ChangeStatus(
                incidentId,
                new ChangeIncidentStatusDto { NewStatus = "Zakończone" },
                null);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}