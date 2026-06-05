using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using _211system.Controllers;
using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using CPR112.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Police;
using Xunit;

namespace _211system.Tests
{
    public class PoliceControllerTests
    {
        private async Task<_211DbContext> GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new _211DbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        private Mock<IAuthService> GetMockAuthService()
        {
            var mock = new Mock<IAuthService>();
            mock.Setup(s => s.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(("mock-acc", "Temp1234"));
            return mock;
        }

        private PoliceService CreatePoliceService(_211DbContext db)
        {
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return new PoliceService(db, GetMockAuthService().Object, httpMock.Object);
        }

        private PoliceController CreateController(_211DbContext db, IPoliceService? service = null)
        {
            return new PoliceController(service ?? CreatePoliceService(db), db);
        }

        private static void SeedIncidentTypes(_211DbContext db)
        {
            if (!db.SeverityLevels.Any())
            {
                db.SeverityLevels.Add(new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" });
            }

            if (!db.IncidentTypes.Any())
            {
                db.IncidentTypes.Add(new IncidentType { Id = 1, Name = "Wypadek" });
            }

            db.SaveChanges();
        }

        private static async Task<Guid> SeedDepartmentAsync(_211DbContext db)
        {
            var deptId = Guid.NewGuid();
            db.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "KPP Test",
                Address = "ul. Test 1",
                District = "Centrum"
            });
            await db.SaveChangesAsync();
            return deptId;
        }

        private static string? GetBadRequestMessage(IActionResult result)
        {
            if (result is not BadRequestObjectResult bad || bad.Value == null)
                return null;

            return bad.Value.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        }

        [Fact]
        public async Task DeletePoliceman_WhenDriver_ReturnsBadRequest()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            var policemanId = Guid.NewGuid();

            db.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Kierowca",
                Lastname = "Test",
                BadgeNumber = "111",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-driver"
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = Guid.NewGuid(),
                LicensePlate = "WA 100",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var result = await controller.DeletePoliceman(policemanId);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(GetBadRequestMessage(result));
            Assert.NotNull(await db.Policemen.FindAsync(policemanId));
        }

        [Fact]
        public async Task DeleteCar_WhenOnIncident_ReturnsBadRequest()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
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

            var result = await controller.DeleteCar(carId);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(await db.PoliceCars.FindAsync(carId));
        }

        [Fact]
        public async Task FreePoliceCar_WithOpenOps_ClosesOpsAndFrees()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            SeedIncidentTypes(db);

            var carId = Guid.NewGuid();
            var policemanId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var opId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/10",
                Description = "Test",
                Status = "W toku",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0,
                IsPoliceActive = true
            });

            db.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Jan",
                Lastname = "Kowalski",
                BadgeNumber = "1",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-1"
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA FREE",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                CurrentIncidentId = incidentId,
                Status = VehicleOperationalStatus.OnScene,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.PoliceOperations.Add(new PoliceOperation
            {
                Id = opId,
                PDepartmentId = deptId,
                IncidentId = incidentId,
                PolicemanId = policemanId,
                StartTime = DateTime.UtcNow,
                EndTime = null
            });
            await db.SaveChangesAsync();

            var result = await controller.FreePoliceCar(carId);

            Assert.IsType<OkObjectResult>(result);

            var car = await db.PoliceCars.FindAsync(carId);
            var op = await db.PoliceOperations.FindAsync(opId);

            Assert.True(car!.IsAvailable);
            Assert.Equal(VehicleOperationalStatus.InBase, car.Status);
            Assert.Null(car.CurrentIncidentId);
            Assert.NotNull(op!.EndTime);
        }

        [Fact]
        public async Task FreePoliceCar_WithoutPoliceman_ClosesDeptOps()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            SeedIncidentTypes(db);

            var carId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var opId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/11",
                Description = "AI dispatch",
                Status = "W toku",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0,
                IsPoliceActive = true
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA AI",
                PDepartmentId = deptId,
                PolicemanId = null,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                CurrentIncidentId = incidentId,
                Status = VehicleOperationalStatus.EnRouteToIncident,
                Latitude = 52.1,
                Longitude = 21.1
            });

            db.PoliceOperations.Add(new PoliceOperation
            {
                Id = opId,
                PDepartmentId = deptId,
                IncidentId = incidentId,
                PolicemanId = null,
                StartTime = DateTime.UtcNow,
                EndTime = null
            });
            await db.SaveChangesAsync();

            var result = await controller.FreePoliceCar(carId);

            Assert.IsType<OkObjectResult>(result);

            var op = await db.PoliceOperations.FindAsync(opId);
            Assert.NotNull(op!.EndTime);
        }

        [Fact]
        public async Task StartOperation_Valid_ReturnsOk()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            SeedIncidentTypes(db);

            var policemanId = Guid.NewGuid();
            var carId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            db.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Start",
                Lastname = "Ops",
                BadgeNumber = "2",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-2"
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA START",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/12",
                Description = "Nowe",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var result = await controller.StartOperation(policemanId, incidentId);

            Assert.IsType<OkObjectResult>(result);

            var car = await db.PoliceCars.FindAsync(carId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.Equal(incidentId, car!.CurrentIncidentId);
            Assert.Equal("W toku", incident!.Status);
        }

        [Fact]
        public async Task EndOperation_SetsEndTime()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            SeedIncidentTypes(db);

            var policemanId = Guid.NewGuid();
            var carId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var opId = Guid.NewGuid();

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/13",
                Description = "Koniec",
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

            db.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "End",
                Lastname = "Ops",
                BadgeNumber = "3",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-3"
            });

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA END",
                PDepartmentId = deptId,
                PolicemanId = policemanId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = false,
                CurrentIncidentId = incidentId,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.PoliceOperations.Add(new PoliceOperation
            {
                Id = opId,
                PDepartmentId = deptId,
                IncidentId = incidentId,
                PolicemanId = policemanId,
                StartTime = DateTime.UtcNow,
                EndTime = null
            });
            await db.SaveChangesAsync();

            var result = await controller.EndOperation(opId);

            Assert.IsType<OkObjectResult>(result);

            var op = await db.PoliceOperations.FindAsync(opId);
            var car = await db.PoliceCars.FindAsync(carId);
            var incident = await db.Incidents.FindAsync(incidentId);

            Assert.NotNull(op!.EndTime);
            Assert.True(car!.IsAvailable);
            Assert.Null(car.CurrentIncidentId);
            Assert.False(incident!.IsPoliceActive);
            Assert.Equal("Zakończone", incident.Status);
        }

        [Fact]
        public async Task AddCarEquipment_Get_Delete()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            var carId = Guid.NewGuid();

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA EQ",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var addResult = await controller.AddCarEquipment(carId, new PoliceController.EquipmentDto
            {
                Name = "Latarka",
                Quantity = 2
            });

            var addOk = Assert.IsType<OkObjectResult>(addResult);
            var added = Assert.IsType<PoliceEquipment>(addOk.Value);
            Assert.Equal("Latarka", added.Name);

            var getResult = await controller.GetCarEquipment(carId);
            var list = Assert.IsType<OkObjectResult>(getResult).Value as System.Collections.Generic.List<PoliceEquipment>;
            Assert.Single(list!);

            var deleteResult = await controller.DeleteCarEquipment(added.Id);
            Assert.IsType<OkResult>(deleteResult);

            var afterDelete = await db.PoliceEquipments.FindAsync(added.Id);
            Assert.Null(afterDelete);
        }

        [Fact]
        public async Task UpdatePoliceCarLocation_UpdatesCoords()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            var carId = Guid.NewGuid();

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA GPS",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0,
                Status = VehicleOperationalStatus.InBase
            });
            await db.SaveChangesAsync();

            var result = await controller.UpdatePoliceCarLocation(carId, new UpdateLocationDto
            {
                Latitude = 52.25,
                Longitude = 21.05,
                Status = (int)VehicleOperationalStatus.OnScene
            });

            Assert.IsType<OkResult>(result);

            var car = await db.PoliceCars.FindAsync(carId);
            Assert.Equal(52.25, car!.Latitude);
            Assert.Equal(21.05, car.Longitude);
            Assert.Equal(VehicleOperationalStatus.OnScene, car.Status);
        }

        [Fact]
        public async Task AssignPoliceCarToIncident_ReturnsOk()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);
            SeedIncidentTypes(db);

            var carId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            db.PoliceCars.Add(new PoliceCar
            {
                Id = carId,
                LicensePlate = "WA ASN",
                PDepartmentId = deptId,
                PoliceEquipmentId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });

            db.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "112/14",
                Description = "Przypisanie",
                Status = "Nowe",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await db.SaveChangesAsync();

            var result = await controller.AssignPoliceCarToIncident(carId, incidentId);

            Assert.IsType<OkObjectResult>(result);
            Assert.False((await db.PoliceCars.FindAsync(carId))!.IsAvailable);
        }

        [Fact]
        public async Task DeleteDepartment_Empty_ReturnsOk()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);

            var result = await controller.DeleteDepartment(deptId);

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(await db.PoliceDepartments.FindAsync(deptId));
        }

        [Fact]
        public async Task DeleteDepartment_WithDependencies_ReturnsBadRequest()
        {
            var db = await GetDatabaseContext();
            var controller = CreateController(db);
            var deptId = await SeedDepartmentAsync(db);

            db.Policemen.Add(new Policeman
            {
                Id = Guid.NewGuid(),
                Name = "Przypisany",
                Lastname = "Funkcjonariusz",
                BadgeNumber = "99",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-99"
            });
            await db.SaveChangesAsync();

            var result = await controller.DeleteDepartment(deptId);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Nie można usunąć", GetBadRequestMessage(result) ?? "");
            Assert.NotNull(await db.PoliceDepartments.FindAsync(deptId));
        }
    }
}
