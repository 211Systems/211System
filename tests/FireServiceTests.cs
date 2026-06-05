using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using CPR112.Models;
using FireDepartment;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace _211system.Tests
{
    public class FireServiceTests
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

        private Mock<IAuthService> GetMockAuthService()
        {
            var mock = new Mock<IAuthService>();
            mock.Setup(s => s.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(("mock-account-id-123", "Temp9999"));
            return mock;
        }

        private FireService CreateService(_211DbContext db)
        {
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return new FireService(db, GetMockAuthService().Object, httpMock.Object);
        }


        private async Task<Guid> SeedDepartmentAsync(_211DbContext db, double lat = 50.0, double lng = 19.0)
        {
            var deptId = Guid.NewGuid();
            db.FireDepartments.Add(new FDepartment
            {
                FDepartmentId = deptId,
                Name = "JRG",
                Address = "ul. Test",
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
        public async Task CreateDepartmentAsync_ShouldAddFireDepartmentToDatabase()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var result = await service.CreateDepartmentAsync(new CreateFDepartmentDto
            {
                Name = "JRG 1",
                Address = "ul. Strażacka 998",
                District = "Centrum"
            });

            var departmentInDb = await db.FireDepartments.FirstOrDefaultAsync(d => d.FDepartmentId == result.FDepartmentId);

            Assert.NotNull(result);
            Assert.NotNull(departmentInDb);
            Assert.Equal("JRG 1", departmentInDb.Name);
        }

        [Fact]
        public async Task CreateFiremanAsync_WithValidDepartment_ShouldAddFireman()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var department = await service.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "OSP Test", Address = "Test", District = "Test" });

            var result = await service.CreateFiremanAsync(new CreateFiremanDto
            {
                Name = "Piotr",
                Lastname = "Zalewski",
                BadgeNumber = "PSP-998",
                Rank = "Kapitan",
                FDepartmentId = department.FDepartmentId,
                Email = "piotr@straz.pl"
            });

            Assert.NotNull(result);
            Assert.Equal("piotr@straz.pl", result.Email);
            Assert.Equal("Temp9999", result.TemporaryPassword);
            Assert.Equal(1, await db.Firemen.CountAsync());
        }

        [Fact]
        public async Task CreateFiremanAsync_WithInvalidDepartment_ShouldThrowException()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateFiremanAsync(new CreateFiremanDto
            {
                Name = "Adam",
                Lastname = "Brakujący",
                BadgeNumber = "PSP-000",
                Rank = "Strazak",
                FDepartmentId = Guid.NewGuid(),
                Email = "adam@straz.pl"
            }));

            Assert.Equal("Remiza o podanym ID nie istnieje!", ex.Message);
        }

        [Fact]
        public async Task GetAllDepartmentsAsync_ShouldReturnAllDepartmentsAsDtos()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);

            await service.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 1", Address = "A1", District = "D1" });
            await service.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 2", Address = "A2", District = "D2" });

            var result = await service.GetAllDepartmentsAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, d => d.Name == "JRG 2");
        }



        [Fact]
        public async Task DeleteFiremanAsync_WhenDriverOnTruck_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);
            var firemanId = Guid.NewGuid();

            db.Firemen.Add(new Fireman
            {
                Id = firemanId,
                Name = "Kierowca",
                Lastname = "Wozu",
                BadgeNumber = "2",
                Rank = "Strazak",
                FDepartmentId = deptId,
                FireAccountId = "acc-2"
            });

            db.FireTrucks.Add(new FireTruck
            {
                Id = Guid.NewGuid(),
                LicensePlate = "STR 1",
                FDepartmentId = deptId,
                FiremanId = firemanId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 50.0,
                Longitude = 19.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteFiremanAsync(firemanId));
            Assert.NotNull(await db.Firemen.FindAsync(firemanId));
        }

        [Fact]
        public async Task DeleteFiremanAsync_WhenFree_Removes()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);
            var firemanId = Guid.NewGuid();

            db.Firemen.Add(new Fireman
            {
                Id = firemanId,
                Name = "Wolny",
                Lastname = "Strazak",
                BadgeNumber = "3",
                Rank = "Strazak",
                FDepartmentId = deptId,
                FireAccountId = "acc-3"
            });

            db.FireOperations.Add(new FireDepartmentOperation
            {
                Id = Guid.NewGuid(),
                FiremanId = firemanId,
                FDepartmentId = deptId,
                IncidentId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await service.DeleteFiremanAsync(firemanId);

            Assert.Null(await db.Firemen.FindAsync(firemanId));
            Assert.False(await db.FireOperations.AnyAsync(o => o.FiremanId == firemanId));
        }

        [Fact]
        public async Task CreateFireTruckAsync_Valid()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db, 50.5, 19.5);

            var result = await service.CreateFireTruckAsync(new CreateFireTruckDto
            {
                LicensePlate = "STR 100",
                FDepartmentId = deptId
            });

            Assert.Equal("STR 100", result.LicensePlate);
            Assert.Equal(50.5, result.Latitude);
            Assert.Equal(19.5, result.Longitude);
            Assert.True(result.IsAvailable);
        }

        [Fact]
        public async Task UpdateFireTruckAsync_Valid()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var truckId = Guid.NewGuid();
            var firemanId = Guid.NewGuid();

            db.Firemen.Add(new Fireman
            {
                Id = firemanId,
                Name = "Nowy",
                Lastname = "Kierowca",
                BadgeNumber = "4",
                Rank = "Strazak",
                FDepartmentId = deptId,
                FireAccountId = "acc-4"
            });

            db.FireTrucks.Add(new FireTruck
            {
                Id = truckId,
                LicensePlate = "STARE",
                FDepartmentId = deptId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 50.0,
                Longitude = 19.0
            });
            await db.SaveChangesAsync();

            await service.UpdateFireTruckAsync(truckId, new UpdateFireTruckDto
            {
                LicensePlate = "NOWE",
                FiremanId = firemanId
            });

            var truck = await db.FireTrucks.FindAsync(truckId);
            Assert.Equal("NOWE", truck.LicensePlate);
            Assert.Equal(firemanId, truck.FiremanId);
        }

        [Fact]
        public async Task DeleteFireTruckAsync_WhenOnIncident_Throws()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var truckId = Guid.NewGuid();
            db.FireTrucks.Add(new FireTruck
            {
                Id = truckId,
                LicensePlate = "STR INC",
                FDepartmentId = deptId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = true,
                CurrentIncidentId = Guid.NewGuid(),
                Latitude = 50.0,
                Longitude = 19.0
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteFireTruckAsync(truckId));
        }

        [Fact]
        public async Task DeleteFireTruckAsync_WhenInBase_Removes()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var truckId = Guid.NewGuid();
            db.FireTrucks.Add(new FireTruck
            {
                Id = truckId,
                LicensePlate = "STR DEL",
                FDepartmentId = deptId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 50.0,
                Longitude = 19.0
            });
            await db.SaveChangesAsync();

            await service.DeleteFireTruckAsync(truckId);

            Assert.Null(await db.FireTrucks.FindAsync(truckId));
        }

        [Fact]
        public async Task AssignFireTruckToIncidentAsync_SetsState()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            SeedSeverity(db);
            var deptId = await SeedDepartmentAsync(db);

            var truckId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            db.FireTrucks.Add(new FireTruck
            {
                Id = truckId,
                LicensePlate = "STR ACT",
                FDepartmentId = deptId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 50.0,
                Longitude = 19.0
            });

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/1",
                Description = "Pożar",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 50.0,
                Longitude = 19.0
            });
            await db.SaveChangesAsync();

            await service.AssignFireTruckToIncidentAsync(truckId, incidentId);

            var truck = await db.FireTrucks.FindAsync(truckId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.False(truck.IsAvailable);
            Assert.Equal(incidentId, truck.CurrentIncidentId);
            Assert.Equal("W toku", incident.Status);
            Assert.True(incident.IsFireActive);
        }

        [Fact]
        public async Task ReturnToBaseAsync_FreesTruck()
        {
            var db = await GetDatabaseContext();
            var service = CreateService(db);
            var deptId = await SeedDepartmentAsync(db);

            var operationId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var firemanId = Guid.NewGuid();
            var truckId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/2",
                Description = "Test",
                Status = "W toku",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 50.0,
                Longitude = 19.0,
                IsFireActive = true,
                IsPoliceActive = false,
                IsMedicalActive = false
            });

            db.FireOperations.Add(new FireDepartmentOperation
            {
                Id = operationId,
                FDepartmentId = deptId,
                IncidentId = incidentId,
                FiremanId = firemanId,
                StartTime = DateTime.UtcNow
            });

            db.FireTrucks.Add(new FireTruck
            {
                Id = truckId,
                LicensePlate = "STR RET",
                FDepartmentId = deptId,
                FiremanId = firemanId,
                CurrentIncidentId = incidentId,
                FireEquipmentid = Guid.NewGuid(),
                IsAvailable = false,
                Latitude = 50.0,
                Longitude = 19.0,
                Status = VehicleOperationalStatus.OnScene
            });
            await db.SaveChangesAsync();

            await service.ReturnToBaseAsync(operationId);

            var truck = await db.FireTrucks.FindAsync(truckId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.Equal(VehicleOperationalStatus.ReturningToBase, truck.Status);
            Assert.False(incident.IsFireActive);
            Assert.Equal("Zakończone", incident.Status);
        }
    }
}