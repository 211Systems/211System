using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _211system.Controllers;
using _211system.Data;
using _211system.DTOs.Ai;
using _211system.Models;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using _211system.Services;
using CPR112.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _211system.Tests
{
    public class AiControllerTests
    {
        private _211DbContext GetDb()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new _211DbContext(options);

            if (!db.SeverityLevels.Any())
            {
                db.SeverityLevels.Add(new SeverityLevel { Id = 1, Name = "Wysoki", ColorCode = "danger" });
                db.IncidentTypes.Add(new IncidentType { Id = 1, Name = "Wypadek", RequiresPolice = true, RequiresMedic = true, RequiresFire = false });
                db.SaveChanges();
            }

            return db;
        }

        private static AiController CreateController(
            _211DbContext db,
            Mock<IAiService>? aiMock = null,
            Mock<IWeatherService>? weatherMock = null)
        {
            aiMock ??= new Mock<IAiService>();
            weatherMock ??= new Mock<IWeatherService>();
            var logger = new Mock<ILogger<AiController>>();

            return new AiController(db, aiMock.Object, weatherMock.Object, logger.Object);
        }

        [Fact]
        public async Task AiController_GenerateDispatchPlan_NoNewIncidents_ReturnsEmpty()
        {
            var db = GetDb();
            var aiMock = new Mock<IAiService>();

            db.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "1",
                Description = "Stary",
                Status = "Zakończone",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db, aiMock);
            var result = await controller.GenerateDispatchPlan();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<AiDispatchSuggestion>>(ok.Value);
            Assert.Empty(list);
            aiMock.Verify(a => a.GetAutoDispatchPlanAsync(It.IsAny<AiDispatchRequestDto>()), Times.Never);
        }

        [Fact]
        public async Task AiController_GenerateDispatchPlan_WithIncidents_CallsAiService()
        {
            var db = GetDb();
            var aiMock = new Mock<IAiService>();
            var incidentId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/1",
                Description = "Nowy",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Ambulances.Add(new Ambulance
            {
                Id = Guid.NewGuid(),
                Type = AmbulanceType.S,
                LicensePlate = "GD 1",
                HospitalId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            aiMock.Setup(a => a.GetAutoDispatchPlanAsync(It.IsAny<AiDispatchRequestDto>()))
                .ReturnsAsync(new List<AiDispatchSuggestion>
                {
                    new AiDispatchSuggestion { IncidentId = incidentId, UnitId = Guid.NewGuid(), UnitType = "Medical" }
                });

            var controller = CreateController(db, aiMock);
            var result = await controller.GenerateDispatchPlan();

            Assert.IsType<OkObjectResult>(result);
            aiMock.Verify(a => a.GetAutoDispatchPlanAsync(It.Is<AiDispatchRequestDto>(d => d.Incidents.Any())), Times.Once);
        }

        [Fact]
        public async Task AiController_ConfirmDispatchPlan_CreatesOperations()
        {
            var db = GetDb();
            var incidentId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();
            var paramedicId = Guid.NewGuid();
            var hospitalId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/2",
                Description = "Do akcji",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Jan",
                LastName = "Ratownik",
                LicenseNumber = "PWZ1",
                Specialization = "Medycyna",
                Rank = "Medyk",
                ParaAccountId = "acc-1",
                HospitalId = hospitalId
            });

            db.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD 2",
                HospitalId = hospitalId,
                ParamedicId = paramedicId,
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var result = await controller.ConfirmDispatchPlan(new List<AiDispatchSuggestion>
            {
                new AiDispatchSuggestion
                {
                    IncidentId = incidentId,
                    UnitId = ambulanceId,
                    UnitType = "Medical"
                }
            });

            Assert.IsType<OkObjectResult>(result);

            var amb = await db.Ambulances.FindAsync(ambulanceId);
            Assert.False(amb.IsAvailable);
            Assert.Equal(incidentId, amb.CurrentIncidentId);
            Assert.True(await db.MedicalOperations.AnyAsync(o => o.ReportId == incidentId && o.ParamedicId == paramedicId));
        }

        [Fact]
        public async Task AiController_ConfirmDispatchPlan_InvalidSuggestion_ReturnsBadRequest()
        {
            var db = GetDb();
            var controller = CreateController(db);

            var emptyResult = await controller.ConfirmDispatchPlan(new List<AiDispatchSuggestion>());
            Assert.IsType<BadRequestObjectResult>(emptyResult);

            var incidentId = Guid.NewGuid();
            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/3",
                Description = "X",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 0,
                Longitude = 0
            });
            await db.SaveChangesAsync();

            var badUnit = await controller.ConfirmDispatchPlan(new List<AiDispatchSuggestion>
            {
                new AiDispatchSuggestion { IncidentId = incidentId, UnitId = Guid.NewGuid(), UnitType = "NieznanyTyp" }
            });

            Assert.IsType<BadRequestObjectResult>(badUnit);
        }

        [Fact]
        public async Task AiController_ConfirmDispatchPlan_WhenAiUnavailable_Returns503()
        {
            var db = GetDb();
            var aiMock = new Mock<IAiService>();

            db.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "112/4",
                Description = "Nowy",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Ambulances.Add(new Ambulance
            {
                Id = Guid.NewGuid(),
                Type = AmbulanceType.S,
                LicensePlate = "GD 3",
                HospitalId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            aiMock.Setup(a => a.GetAutoDispatchPlanAsync(It.IsAny<AiDispatchRequestDto>()))
                .ThrowsAsync(new AiServiceUnavailableException("Model przeciazony", upstreamStatusCode: 503));

            var controller = CreateController(db, aiMock);
            var result = await controller.GenerateDispatchPlan();

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, status.StatusCode);
        }
    }
}