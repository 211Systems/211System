using _211system.Data;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using _211system.Models;

namespace tests
{
    public class MedicalServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new _211DbContext(options);
        }

        [Fact]
        public async Task StartMedicalOperationAsync_Should_Start_When_Paramedic_Is_Free()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);
            
            var paramedicId = Guid.NewGuid();
            var reportId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Test",
                LastName = "Test",
                LicenseNumber = "123",
                Specialization = "Medycyna", 
                Rank = "Medyk",
                ParaAccountId = "konto",
                HospitalId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var operationId = await service.StartMedicalOperationAsync(paramedicId, reportId);

            var operationInDb = await context.MedicalOperations.FindAsync(operationId);
            Assert.NotNull(operationInDb);
            Assert.Null(operationInDb.EndTime);
            Assert.Equal(paramedicId, operationInDb.ParamedicId);
        }

        [Fact]
        public async Task StartMedicalOperationAsync_Should_Throw_When_Paramedic_Is_Busy()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);
            
            var paramedicId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Test",
                LastName = "Test",
                LicenseNumber = "123",
                Specialization = "Medycyna",
                Rank = "Medyk",
                ParaAccountId = "konto",
                HospitalId = Guid.NewGuid()
            });

            context.MedicalOperations.Add(new MedicalOperation
            {
                ParamedicId = paramedicId,
                ReportId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = null
            });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartMedicalOperationAsync(paramedicId, Guid.NewGuid()));

            Assert.Equal("Ten ratownik jest już przypisany do innej, niezakończonej akcji!", exception.Message);
        }

        [Fact]
        public async Task EndMedicalOperationAsync_Should_Set_EndTime()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);
            
            var operationId = Guid.NewGuid();

            context.MedicalOperations.Add(new MedicalOperation
            {
                Id = operationId,
                ParamedicId = Guid.NewGuid(),
                ReportId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null
            });
            await context.SaveChangesAsync();

            await service.EndMedicalOperationAsync(operationId);

            var operationInDb = await context.MedicalOperations.FindAsync(operationId);
            Assert.NotNull(operationInDb.EndTime);
        }

        [Fact]
        public async Task GetAllHospitalsAsync_Should_Return_All_Hospitals()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);

            context.Hospitals.AddRange(
                new Hospital { Id = Guid.NewGuid(), Name = "Szpital A", Address = "Adres A", HasSOR = true },
                new Hospital { Id = Guid.NewGuid(), Name = "Szpital B", Address = "Adres B", HasSOR = false }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAllHospitalsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, h => h.Name == "Szpital A");
        }

        [Fact]
        public async Task GetAllParamedicsAsync_Should_Return_All_Paramedics_With_Emails()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);

            var accountId = Guid.NewGuid().ToString();
            var account = new ApplicationUser { Id = accountId, Email = "ratownik@szpital.pl" };
            context.Users.Add(account);

            context.Paramedics.Add(new Paramedic
            {
                Id = Guid.NewGuid(),
                Name = "Jan",
                LastName = "Kowalski",
                LicenseNumber = "PWZ123",
                Specialization = "Medycyna",
                Rank = "Lekarz",
                ParaAccountId = accountId,
                HospitalId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var result = await service.GetAllParamedicsAsync();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("ratownik@szpital.pl", result.First().Email);
        }

        [Fact]
        public async Task CreateAmbulanceAsync_Should_Add_Ambulance_To_Database()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);

            var hospitalId = Guid.NewGuid();
            var dto = new _211system.DTOs.Hospital.CreateAmbulanceDto
            {
                Type = AmbulanceType.S,
                LicensePlate = "GD 12345",
                HospitalId = hospitalId,
                ParamedicId = null
            };

            var result = await service.CreateAmbulanceAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("GD 12345", result.LicensePlate);
            Assert.Equal(AmbulanceType.S, result.Type);

            var inDb = await context.Ambulances.FindAsync(result.Id);
            Assert.NotNull(inDb);
            Assert.Equal(hospitalId, inDb.HospitalId);
        }

        [Fact]
        public async Task GetAllAmbulancesAsync_Should_Return_All_Ambulances_With_Hospital_GPS()
        {
            var context = GetInMemoryDbContext();
            var mockAuthService = new Mock<IAuthService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var service = new MedicalService(context, mockAuthService.Object, mockHttpClientFactory.Object);

            var hospital1 = new Hospital
            {
                Id = Guid.NewGuid(),
                Name = "Szpital 1",
                Address = "ul. Testowa 1",
                Latitude = 52.0,
                Longitude = 21.0
            };
            var hospital2 = new Hospital
            {
                Id = Guid.NewGuid(),
                Name = "Szpital 2",
                Address = "ul. Testowa 2",
                Latitude = 51.0,
                Longitude = 19.0
            };
            context.Hospitals.AddRange(hospital1, hospital2);

            context.Ambulances.AddRange(
                new Ambulance { Id = Guid.NewGuid(), Type = AmbulanceType.P, LicensePlate = "POZ 111", HospitalId = hospital1.Id },
                new Ambulance { Id = Guid.NewGuid(), Type = AmbulanceType.N, LicensePlate = "POZ 222", HospitalId = hospital2.Id }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAllAmbulancesAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());

            var amb1 = result.First(a => a.LicensePlate == "POZ 111");
            Assert.Equal(52.0, amb1.Latitude);
            Assert.Equal(21.0, amb1.Longitude);
        }
    }
}