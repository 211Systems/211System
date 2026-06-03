using _211system.Data;
using _211system.Models;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace _211system.Tests
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

        private MedicalService CreateService(_211DbContext context)
        {
            var authMock = new Mock<IAuthService>();
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return new MedicalService(context, authMock.Object, httpMock.Object);
        }

        [Fact]
        public async Task StartMedicalOperationAsync_Should_Start_When_Paramedic_Is_Free()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
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
            var service = CreateService(context);
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

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartMedicalOperationAsync(paramedicId, Guid.NewGuid()));

            Assert.Equal("Ten ratownik jest już przypisany do innej, niezakończonej akcji!", ex.Message);
        }

        [Fact]
        public async Task EndMedicalOperationAsync_Should_Set_EndTime()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
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
            var service = CreateService(context);

            context.Hospitals.AddRange(
                new Hospital { Id = Guid.NewGuid(), Name = "Szpital A", Address = "Adres A", HasSOR = true },
                new Hospital { Id = Guid.NewGuid(), Name = "Szpital B", Address = "Adres B", HasSOR = false }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAllHospitalsAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, h => h.Name == "Szpital A");
        }

        [Fact]
        public async Task GetAllParamedicsAsync_Should_Return_All_Paramedics_With_Emails()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var accountId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser { Id = accountId, Email = "ratownik@szpital.pl", UserName = "ratownik@szpital.pl" });

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

            Assert.Single(result);
            Assert.Equal("ratownik@szpital.pl", result.First().Email);
        }

        [Fact]
        public async Task CreateAmbulanceAsync_Should_Add_Ambulance_To_Database()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var hospitalId = Guid.NewGuid();
            context.Hospitals.Add(new Hospital
            {
                Id = hospitalId,
                Name = "Szpital Główny",
                Address = "ul. Ratownicza 1",
                Latitude = 52.2297,
                Longitude = 21.0122
            });
            await context.SaveChangesAsync();

            var result = await service.CreateAmbulanceAsync(new _211system.DTOs.Hospital.CreateAmbulanceDto
            {
                Type = AmbulanceType.S,
                LicensePlate = "GD 12345",
                HospitalId = hospitalId,
                ParamedicId = null
            });

            Assert.Equal("GD 12345", result.LicensePlate);
            Assert.Equal(52.2297, result.Latitude);
            Assert.Equal(21.0122, result.Longitude);

            var inDb = await context.Ambulances.FindAsync(result.Id);
            Assert.NotNull(inDb);
            Assert.Equal(hospitalId, inDb.HospitalId);
        }

        [Fact]
        public async Task GetAllAmbulancesAsync_Should_Return_All_Ambulances_With_Their_Own_GPS()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var hospital1 = new Hospital
            {
                Id = Guid.NewGuid(),
                Name = "Szpital 1",
                Address = "ul. Testowa 1",
                Latitude = 52.0,
                Longitude = 21.0
            };
            context.Hospitals.Add(hospital1);

            context.Ambulances.AddRange(
                new Ambulance
                {
                    Id = Guid.NewGuid(),
                    Type = AmbulanceType.P,
                    LicensePlate = "POZ 111",
                    HospitalId = hospital1.Id,
                    Latitude = 52.3333,
                    Longitude = 21.1111,
                    Status = VehicleOperationalStatus.EnRouteToIncident
                },
                new Ambulance
                {
                    Id = Guid.NewGuid(),
                    Type = AmbulanceType.N,
                    LicensePlate = "POZ 222",
                    HospitalId = hospital1.Id,
                    Latitude = 51.5555,
                    Longitude = 19.2222,
                    Status = VehicleOperationalStatus.InBase
                }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAllAmbulancesAsync();

            Assert.Equal(2, result.Count());
            var amb1 = result.First(a => a.LicensePlate == "POZ 111");
            Assert.Equal(52.3333, amb1.Latitude);
            Assert.Equal(21.1111, amb1.Longitude);
        }

        [Fact]
        public async Task TransportToHospitalAsync_SetsTarget()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var operationId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();

            context.MedicalOperations.Add(new MedicalOperation
            {
                Id = operationId,
                ReportId = incidentId,
                ParamedicId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });

            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD TR",
                HospitalId = Guid.NewGuid(),
                CurrentIncidentId = incidentId,
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.TransportToHospitalAsync(operationId, Guid.NewGuid());

            var amb = await context.Ambulances.FindAsync(ambulanceId);
            Assert.Equal(VehicleOperationalStatus.Transporting, amb.Status);
        }

        [Fact]
        public async Task ReturnToBaseAsync_EndsOperation()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var operationId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();

            context.Incidents.Add(new CPR112.Models.Incident
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
                IsMedicalActive = true,
                IsPoliceActive = false,
                IsFireActive = false
            });

            context.MedicalOperations.Add(new MedicalOperation
            {
                Id = operationId,
                ReportId = incidentId,
                ParamedicId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });

            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD RET",
                HospitalId = Guid.NewGuid(),
                CurrentIncidentId = incidentId,
                IsAvailable = false,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.ReturnToBaseAsync(operationId);

            var amb = await context.Ambulances.FindAsync(ambulanceId);
            var incident = await context.Incidents.FindAsync(incidentId);

            Assert.Equal(VehicleOperationalStatus.ReturningToBase, amb.Status);
            Assert.False(incident.IsMedicalActive);
            Assert.Equal("Zakończone", incident.Status);
        }

        [Fact]
        public async Task GetAllOperationsAsync_ReturnsOpenAndClosed()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var paramedicId = Guid.NewGuid();
            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "A",
                LastName = "B",
                LicenseNumber = "1",
                Specialization = "Med",
                Rank = "Medyk",
                ParaAccountId = "acc",
                HospitalId = Guid.NewGuid()
            });

            context.MedicalOperations.AddRange(
                new MedicalOperation
                {
                    Id = Guid.NewGuid(),
                    ParamedicId = paramedicId,
                    ReportId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    EndTime = DateTime.UtcNow.AddHours(-1)
                },
                new MedicalOperation
                {
                    Id = Guid.NewGuid(),
                    ParamedicId = paramedicId,
                    ReportId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow,
                    EndTime = null
                }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAllOperationsAsync();

            Assert.Equal(2, result.Count());
            Assert.Single(result, o => o.EndTime == null);
            Assert.Single(result, o => o.EndTime != null);
        }
    }
}