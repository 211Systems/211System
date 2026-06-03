using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Police;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace _211system.Tests
{
    public class PoliceCarServiceTests
    {
        private async Task<_211DbContext> GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var databaseContext = new _211DbContext(options);
            await databaseContext.Database.EnsureCreatedAsync();
            return databaseContext;
        }

        private Mock<IAuthService> GetMockAuthService()
        {
            var mock = new Mock<IAuthService>();
            mock.Setup(s => s.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(("mock-acc", "Temp1234"));
            return mock;
        }

        private PoliceService CreateService(_211DbContext db)
        {
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return new PoliceService(db, GetMockAuthService().Object, httpMock.Object);
        }

        private async Task<Guid> SeedDepartmentAsync(_211DbContext db, double lat = 52.1, double lng = 21.0)
        {
            var deptId = Guid.NewGuid();
            db.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "KPP",
                Address = "ul. Test 1",
                District = "Centrum",
                Latitude = lat,
                Longitude = lng
            });
            await db.SaveChangesAsync();
            return deptId;
        }

        private void SeedSeverity(_211DbContext db)
        {
            if (!db.SeverityLevels.Any())
            {
                db.SeverityLevels.Add(new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" });
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task CreatePoliceCarAsync_Valid_AddsCarAtDepartmentCoords()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db, 52.5, 21.5);

            var result = await service.CreatePoliceCarAsync(new CreatePoliceCarDto
            {
                LicensePlate = "WA 12345",
                PDepartmentId = deptId
            });

            Assert.Equal("WA 12345", result.LicensePlate);
            Assert.Equal(52.5, result.Latitude);
            Assert.Equal(21.5, result.Longitude);
            Assert.True(result.IsAvailable);
        }

        [Fact]
        public async Task CreatePoliceCarAsync_InvalidDepartment_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var ex = await Assert.ThrowsAsync<Exception>(() => service.CreatePoliceCarAsync(new CreatePoliceCarDto
            {
                LicensePlate = "WA 999",
                PDepartmentId = Guid.NewGuid()
            }));

            Assert.Contains("Komenda", ex.Message);
        }

        [Fact]
        public async Task GetAllPoliceCarsAsync_ReturnsMappedDtos()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            db.PoliceCars.Add(new PoliceCar
            {
                Id = Guid.NewGuid(),
                LicensePlate = "WA 111",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.1,
                Longitude = 21.0,
                Status = VehicleOperationalStatus.InBase
            });
            await db.SaveChangesAsync();

            var result = await service.GetAllPoliceCarsAsync();

            Assert.Single(result);
            Assert.Equal("WA 111", result.First().LicensePlate);
            Assert.Equal(deptId, result.First().PDepartmentId);
        }

        [Fact]
        public async Task UpdatePoliceCarAsync_ChangesPlateAndDriver()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            var policemanId = Guid.NewGuid();

            db.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Jan",
                Lastname = "Nowak",
                BadgeNumber = "1",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-1"
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "STARE",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await service.UpdatePoliceCarAsync(carId, new UpdatePoliceCarDto
            {
                LicensePlate = "NOWE",
                PolicemanId = policemanId
            });

            var car = await db.PoliceCars.FindAsync(carId);
            Assert.Equal("NOWE", car.LicensePlate);
            Assert.Equal(policemanId, car.PolicemanId);
        }

        [Fact]
        public async Task DeletePoliceCarAsync_WhenAvailableAndNoIncident_Removes()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA DEL",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await service.DeletePoliceCarAsync(carId);

            Assert.Null(await db.PoliceCars.FindAsync(carId));
        }

        [Fact]
        public async Task DeletePoliceCarAsync_WhenNotAvailable_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA BUSY",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeletePoliceCarAsync(carId));
        }

        [Fact]
        public async Task DeletePoliceCarAsync_WhenOnIncident_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA INC",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                CurrentIncidentId = Guid.NewGuid(),
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeletePoliceCarAsync(carId));
        }

        [Fact]
        public async Task AssignPoliceCarToIncidentAsync_SetsBusyAndLinksIncident()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            SeedSeverity(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA ACT",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/1",
                Description = "Test",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await service.AssignPoliceCarToIncidentAsync(carId, incidentId);

            var car = await db.PoliceCars.FindAsync(carId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.False(car.IsAvailable);
            Assert.Equal(incidentId, car.CurrentIncidentId);
            Assert.Equal("W toku", incident.Status);
            Assert.True(incident.IsPoliceActive);
        }

        [Fact]
        public async Task AssignPoliceCarAsync_WhenAlreadyBusy_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA ZAJETY",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                CurrentIncidentId = Guid.NewGuid(),
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AssignPoliceCarToIncidentAsync(carId, Guid.NewGuid()));
        }

        [Fact]
        public async Task AssignPoliceCarAsync_WhenIncidentNotFound_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var carId = Guid.NewGuid();
            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA NOINC",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AssignPoliceCarToIncidentAsync(carId, Guid.NewGuid()));
        }
    }
}
