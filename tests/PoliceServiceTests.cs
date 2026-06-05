using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Police;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tests;
using Xunit;

namespace _211system.Tests
{
    public class PoliceServiceTests
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
                .ReturnsAsync(("mock-police-acc-1", "Temp1234"));
            return mock;
        }

        private PoliceService CreateService(_211DbContext db, Mock<IAuthService>? authMock = null)
        {
            authMock ??= GetMockAuthService();
            var httpMock = new Mock<IHttpClientFactory>();
            return new PoliceService(db, authMock.Object, httpMock.Object, TestServiceMocks.CreateTransportService().Object);
        }

        [Fact]
        public async Task CreateDepartmentAsync_ShouldAddDepartmentToDatabase()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var dto = new CreatePDepartmentDto
            {
                Name = "Komenda Główna",
                Address = "ul. Puławska 1",
                District = "Mokotów"
            };

            var result = await policeService.CreateDepartmentAsync(dto);
            var departmentInDb = await dbContext.PoliceDepartments.FirstOrDefaultAsync(d => d.PDepartmentId == result.PDepartmentId);

            Assert.NotNull(result);
            Assert.NotNull(departmentInDb);
            Assert.Equal("Komenda Główna", departmentInDb.Name);
        }

        [Fact]
        public async Task CreatePolicemanAsync_WithValidDepartment_ShouldAddPoliceman()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var department = await policeService.CreateDepartmentAsync(new CreatePDepartmentDto { Name = "KPP Test", Address = "Test", District = "Test" });

            var policemanDto = new CreatePolicemanDto
            {
                Name = "Jan",
                Lastname = "Kowalski",
                BadgeNumber = "12345",
                Rank = "Policjant",
                PDepartmentId = department.PDepartmentId,
                Email = "jan@policja.pl"
            };

            var result = await policeService.CreatePolicemanAsync(policemanDto);

            Assert.NotNull(result);
            Assert.Equal("jan@policja.pl", result.Email);
            Assert.Equal("Temp1234", result.TemporaryPassword);
            Assert.Equal(1, await dbContext.Policemen.CountAsync());
        }

        [Fact]
        public async Task CreatePolicemanAsync_WithInvalidDepartment_ShouldThrowException()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var policemanDto = new CreatePolicemanDto
            {
                Name = "Anna",
                Lastname = "Nowak",
                BadgeNumber = "999",
                Rank = "Policjant",
                PDepartmentId = Guid.NewGuid(),
                Email = "anna@policja.pl"
            };

            var exception = await Assert.ThrowsAsync<Exception>(() => policeService.CreatePolicemanAsync(policemanDto));
            Assert.Equal("Komenda o podanym ID nie istnieje!", exception.Message);
        }

        [Fact]
        public async Task GetAllDepartmentsAsync_ShouldReturnAllDepartmentsAsDtos()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            await policeService.CreateDepartmentAsync(new CreatePDepartmentDto { Name = "K1", Address = "A1", District = "D1" });
            await policeService.CreateDepartmentAsync(new CreatePDepartmentDto { Name = "K2", Address = "A2", District = "D2" });

            var result = await policeService.GetAllDepartmentsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, d => d.Name == "K1");
        }

        [Fact]
        public async Task GetAllPolicemenAsync_ReturnsDtosWithEmail()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var deptId = Guid.NewGuid();
            dbContext.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "KPP",
                Address = "Adres",
                District = "Dzielnica"
            });

            var accountId = Guid.NewGuid().ToString();
            dbContext.Users.Add(new ApplicationUser { Id = accountId, Email = "jan@policja.pl", UserName = "jan@policja.pl" });

            dbContext.Policemen.Add(new Policeman
            {
                Id = Guid.NewGuid(),
                Name = "Jan",
                Lastname = "Kowalski",
                BadgeNumber = "111",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = accountId
            });
            await dbContext.SaveChangesAsync();

            var result = await policeService.GetAllPolicemenAsync();

            Assert.Single(result);
            Assert.Equal("jan@policja.pl", result.First().Email);
        }

        [Fact]
        public async Task DeletePolicemanAsync_WhenNotDriver_RemovesAndClearsOperations()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var policemanId = Guid.NewGuid();
            var deptId = Guid.NewGuid();

            dbContext.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "K",
                Address = "A",
                District = "D"
            });

            dbContext.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Usun",
                Lastname = "Mnie",
                BadgeNumber = "222",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-1"
            });

            dbContext.PoliceOperations.Add(new PoliceOperation
            {
                Id = Guid.NewGuid(),
                PolicemanId = policemanId,
                PDepartmentId = deptId,
                IncidentId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            await policeService.DeletePolicemanAsync(policemanId);

            Assert.Null(await dbContext.Policemen.FindAsync(policemanId));
            Assert.False(await dbContext.PoliceOperations.AnyAsync(o => o.PolicemanId == policemanId));
        }

        [Fact]
        public async Task DeletePolicemanAsync_WhenAssignedAsDriver_ThrowsInvalidOperation()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var policemanId = Guid.NewGuid();
            var deptId = Guid.NewGuid();

            dbContext.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "K",
                Address = "A",
                District = "D"
            });

            dbContext.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Kierowca",
                Lastname = "Test",
                BadgeNumber = "333",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-2"
            });

            dbContext.PoliceCars.Add(new PoliceCar
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
            await dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                policeService.DeletePolicemanAsync(policemanId));

            Assert.NotNull(await dbContext.Policemen.FindAsync(policemanId));
        }

        [Fact]
        public async Task DeletePolicemanAsync_WhenNotFound_DoesNothing()
        {
            var dbContext = await GetDatabaseContext();
            var policeService = CreateService(dbContext);

            var policemanId = Guid.NewGuid();
            var deptId = Guid.NewGuid();

            dbContext.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "K",
                Address = "A",
                District = "D"
            });

            dbContext.Policemen.Add(new Policeman
            {
                Id = policemanId,
                Name = "Zostaje",
                Lastname = "Tu",
                BadgeNumber = "444",
                Rank = "Policjant",
                PDepartmentId = deptId,
                PoliceAccountId = "acc-3"
            });
            await dbContext.SaveChangesAsync();

            await policeService.DeletePolicemanAsync(Guid.NewGuid());

            Assert.NotNull(await dbContext.Policemen.FindAsync(policemanId));
            Assert.Equal(1, await dbContext.Policemen.CountAsync());
        }
    }
}