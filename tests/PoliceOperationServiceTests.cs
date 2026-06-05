using _211system.Data;
using _211system.Models;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Police;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using tests;
using Xunit;

namespace _211system.Tests
{
    public class PoliceOperationServiceTests
    {
        private async Task<_211DbContext> GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var db = new _211DbContext(options);
            await db.Database.EnsureCreatedAsync();
            return db;
        }

        private PoliceService CreateService(_211DbContext db)
        {
            var authMock = new Mock<IAuthService>();
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return new PoliceService(db, authMock.Object, httpMock.Object, TestServiceMocks.CreateTransportService().Object);
        }

        [Fact]
        public async Task TransportToStationAsync_UpdatesOperationTarget()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var deptId = Guid.NewGuid();
            var targetDeptId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var policemanId = Guid.NewGuid();
            var carId = Guid.NewGuid();

            db.PoliceDepartments.AddRange(
                new PDepartment { PDepartmentId = deptId, Name = "A", Address = "A", District = "A" },
                new PDepartment { PDepartmentId = targetDeptId, Name = "B", Address = "B", District = "B" }
            );

            db.PoliceOperations.Add(new PoliceOperation
            {
                Id = operationId,
                PDepartmentId = deptId,
                IncidentId = incidentId,
                PolicemanId = policemanId,
                StartTime = DateTime.UtcNow
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA TR",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                CurrentIncidentId = incidentId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0,
                Status = VehicleOperationalStatus.OnScene
            });
            await db.SaveChangesAsync();

            await service.TransportToStationAsync(operationId, targetDeptId);

            var car = await db.PoliceCars.FindAsync(carId);
            Assert.Equal(VehicleOperationalStatus.Transporting, car.Status);
        }

        [Fact]
        public async Task ReturnToBaseAsync_ClosesOperationAndFreesCar()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var deptId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var policemanId = Guid.NewGuid();
            var carId = Guid.NewGuid();

            db.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "KPP",
                Address = "Adres",
                District = "D"
            });

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/1",
                Description = "Test",
                Status = "W toku",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0,
                IsPoliceActive = true,
                IsFireActive = false,
                IsMedicalActive = false
            });

            db.PoliceOperations.Add(new PoliceOperation
            {
                Id = operationId,
                PDepartmentId = deptId,
                IncidentId = incidentId,
                PolicemanId = policemanId,
                StartTime = DateTime.UtcNow
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA RET",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                CurrentIncidentId = incidentId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0,
                Status = VehicleOperationalStatus.OnScene
            });
            await db.SaveChangesAsync();

            await service.ReturnToBaseAsync(operationId);

            var car = await db.PoliceCars.FindAsync(carId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.Equal(VehicleOperationalStatus.ReturningToBase, car.Status);
            Assert.False(incident.IsPoliceActive);
            Assert.Equal("Zakończone", incident.Status);
        }
    }
}
