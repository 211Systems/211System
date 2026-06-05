using _211system.Data;
using _211system.DTOs.Hospital;
using _211system.Models;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using _211system.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tests;
using Xunit;

namespace _211system.Tests
{
    public class MedicalAmbulanceServiceTests
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
            return new MedicalService(context, authMock.Object, httpMock.Object, TestServiceMocks.CreateTransportService().Object);
        }

        private void SeedSeverity(_211DbContext context)
        {
            if (!context.SeverityLevels.Any())
            {
                context.SeverityLevels.Add(new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" });
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAvailableAmbulancesAsync_OnlyFree()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var hospitalId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital
            {
                Id = hospitalId,
                Name = "S",
                Address = "A",
                Latitude = 52.0,
                Longitude = 21.0
            });

            context.Ambulances.AddRange(
                new Ambulance
                {
                    Id = Guid.NewGuid(),
                    Type = AmbulanceType.S,
                    LicensePlate = "WOLNA",
                    HospitalId = hospitalId,
                    IsAvailable = true,
                    Latitude = 52.0,
                    Longitude = 21.0
                },
                new Ambulance
                {
                    Id = Guid.NewGuid(),
                    Type = AmbulanceType.P,
                    LicensePlate = "ZAJETA",
                    HospitalId = hospitalId,
                    IsAvailable = false,
                    CurrentIncidentId = Guid.NewGuid(),
                    Latitude = 52.0,
                    Longitude = 21.0
                }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAvailableAmbulancesAsync();

            Assert.Single(result);
            Assert.Equal("WOLNA", result.First().LicensePlate);
        }

        [Fact]
        public async Task AssignAmbulanceToIncidentAsync_LinksIncident()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            SeedSeverity(context);

            var hospitalId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital { Id = hospitalId, Name = "S", Address = "A", Latitude = 52.0, Longitude = 21.0 });

            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD ACT",
                HospitalId = hospitalId,
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });

            context.Incidents.Add(new Incident
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
            await context.SaveChangesAsync();

            await service.AssignAmbulanceToIncidentAsync(ambulanceId, incidentId);

            var amb = await context.Ambulances.FindAsync(ambulanceId);
            var inc = await context.Incidents.FindAsync(incidentId);

            Assert.False(amb.IsAvailable);
            Assert.Equal(incidentId, amb.CurrentIncidentId);
            Assert.True(inc.IsMedicalActive);
            Assert.Equal("W toku", inc.Status);
        }

        [Fact]
        public async Task UpdateAmbulanceAsync_ChangesDriver()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var hospitalId = Guid.NewGuid();
            var ambulanceId = Guid.NewGuid();
            var paramedicId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital { Id = hospitalId, Name = "S", Address = "A", HasSOR = true });
            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Jan",
                LastName = "K",
                LicenseNumber = "PWZ",
                Specialization = "Med",
                Rank = "Medyk",
                ParaAccountId = "acc",
                HospitalId = hospitalId
            });
            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "STARA",
                HospitalId = hospitalId,
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.UpdateAmbulanceAsync(ambulanceId, new UpdateAmbulanceDto
            {
                LicensePlate = "NOWA",
                Type = AmbulanceType.P,
                ParamedicId = paramedicId
            });

            var amb = await context.Ambulances.FindAsync(ambulanceId);
            Assert.Equal("NOWA", amb.LicensePlate);
            Assert.Equal(paramedicId, amb.ParamedicId);
            Assert.Equal(AmbulanceType.P, amb.Type);
        }

        [Fact]
        public async Task DeleteAmbulanceAsync_WhenOnIncident_Throws()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var ambulanceId = Guid.NewGuid();
            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD INC",
                HospitalId = Guid.NewGuid(),
                IsAvailable = true,
                CurrentIncidentId = Guid.NewGuid(),
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAmbulanceAsync(ambulanceId));
        }

        [Fact]
        public async Task DeleteAmbulanceAsync_WhenFree_Removes()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var ambulanceId = Guid.NewGuid();
            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD DEL",
                HospitalId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await service.DeleteAmbulanceAsync(ambulanceId);

            Assert.Null(await context.Ambulances.FindAsync(ambulanceId));
        }

        [Fact]
        public async Task AddEquipmentAsync_And_GetEquipmentAsync()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var ambulanceId = Guid.NewGuid();

            context.Ambulances.Add(new Ambulance
            {
                Id = ambulanceId,
                Type = AmbulanceType.S,
                LicensePlate = "GD EQ",
                HospitalId = Guid.NewGuid(),
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            var added = await service.AddEquipmentAsync(ambulanceId, new CreateAmbulanceEquipmentDto
            {
                Name = "Defibrylator",
                Quantity = 1
            });

            var list = await service.GetEquipmentAsync(ambulanceId);

            Assert.Equal("Defibrylator", added.Name);
            Assert.Single(list);
            Assert.Equal(1, list.First().Quantity);
        }

        [Fact]
        public async Task DeleteEquipmentAsync_Removes()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var eqId = Guid.NewGuid();

            context.AmbulanceEquipments.Add(new AmbulanceEquipment
            {
                Id = eqId,
                Name = "Torba",
                Quantity = 2,
                AmbulanceId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            await service.DeleteEquipmentAsync(eqId);

            Assert.Null(await context.AmbulanceEquipments.FindAsync(eqId));
        }
    }
}