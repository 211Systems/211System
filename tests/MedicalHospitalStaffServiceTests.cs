using _211system.Data;
using _211system.DTOs.Hospital;
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
    public class MedicalHospitalStaffServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        private MedicalService CreateService(_211DbContext context, Mock<IAuthService>? authMock = null)
        {
            authMock ??= new Mock<IAuthService>();
            authMock.Setup(a => a.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(("med-acc-1", "TempMed99"));
            var httpMock = new Mock<IHttpClientFactory>();
            return new MedicalService(context, authMock.Object, httpMock.Object);
        }

        [Fact]
        public async Task CreateHospitalAsync_AddsHospital()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.CreateHospitalAsync(new CreateHospitalDto
            {
                Name = "Szpital Miejski",
                Address = "ul. Zdrowia 1",
                HasSOR = true,
                Latitude = 52.2,
                Longitude = 21.0
            });

            Assert.Equal("Szpital Miejski", result.Name);
            Assert.True(await context.Hospitals.AnyAsync(h => h.Id == result.Id));
        }

        [Fact]
        public async Task UpdateHospitalAsync_ChangesFields()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var hospitalId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital
            {
                Id = hospitalId,
                Name = "Stary",
                Address = "Stary adres",
                HasSOR = false,
                HasHelipad = false
            });
            await context.SaveChangesAsync();

            await service.UpdateHospitalAsync(hospitalId, new UpdateHospitalDto
            {
                Name = "Nowy",
                Address = "Nowy adres",
                HasSOR = true,
                HasHelipad = true
            });

            var h = await context.Hospitals.FindAsync(hospitalId);
            Assert.Equal("Nowy", h.Name);
            Assert.True(h.HasSOR);
            Assert.True(h.HasHelipad);
        }

        [Fact]
        public async Task DeleteHospitalAsync_Removes()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var hospitalId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital { Id = hospitalId, Name = "X", Address = "Y", HasSOR = false });
            await context.SaveChangesAsync();

            await service.DeleteHospitalAsync(hospitalId);

            Assert.Null(await context.Hospitals.FindAsync(hospitalId));
        }

        [Fact]
        public async Task CreateParamedicAsync_WithAuth_CreatesAccount()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            authMock.Setup(a => a.CreateTemporaryAccountAsync("med@szpital.pl", "Medyk"))
                .ReturnsAsync(("acc-med", "Temp555"));

            var httpMock = new Mock<IHttpClientFactory>();
            var service = new MedicalService(context, authMock.Object, httpMock.Object);

            var hospitalId = Guid.NewGuid();
            context.Hospitals.Add(new Hospital { Id = hospitalId, Name = "S", Address = "A", HasSOR = true });
            await context.SaveChangesAsync();

            var result = await service.CreateParamedicAsync(new CreateParamedicDto
            {
                Name = "Ewa",
                LastName = "Medyk",
                LicenseNumber = "PWZ1",
                Specialization = "Ratownictwo",
                Email = "med@szpital.pl",
                Rank = "Medyk",
                HospitalId = hospitalId
            });

            Assert.Equal("med@szpital.pl", result.Email);
            Assert.Equal("Temp555", result.TemporaryPassword);
            Assert.Equal(1, await context.Paramedics.CountAsync());
        }

        [Fact]
        public async Task UpdateParamedicAsync_Valid()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var id = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = id,
                Name = "Stare",
                LastName = "Nazwisko",
                LicenseNumber = "111",
                Specialization = "Med",
                Rank = "Medyk",
                ParaAccountId = "acc",
                HospitalId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            await service.UpdateParamedicAsync(id, new UpdateParamedicDto
            {
                Name = "Nowe",
                LastName = "Nowak",
                LicenseNumber = "222",
                Rank = "Lekarz"
            });

            var p = await context.Paramedics.FindAsync(id);
            Assert.Equal("Nowe", p.Name);
            Assert.Equal("Lekarz", p.Rank);
        }

        [Fact]
        public async Task DeleteParamedicAsync_WhenDriverOnAmbulance_Throws()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var paramedicId = Guid.NewGuid();
            var hospitalId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Kierowca",
                LastName = "K",
                LicenseNumber = "1",
                Specialization = "Med",
                Rank = "Medyk",
                ParaAccountId = "acc",
                HospitalId = hospitalId
            });

            context.Ambulances.Add(new Ambulance
            {
                Id = Guid.NewGuid(),
                Type = AmbulanceType.S,
                LicensePlate = "GD 1",
                HospitalId = hospitalId,
                ParamedicId = paramedicId,
                IsAvailable = true,
                Latitude = 52.0,
                Longitude = 21.0
            });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteParamedicAsync(paramedicId));
            Assert.NotNull(await context.Paramedics.FindAsync(paramedicId));
        }

        [Fact]
        public async Task DeleteParamedicAsync_WhenFree_Removes()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);
            var paramedicId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Wolny",
                LastName = "M",
                LicenseNumber = "2",
                Specialization = "Med",
                Rank = "Medyk",
                ParaAccountId = "acc2",
                HospitalId = Guid.NewGuid()
            });

            context.MedicalOperations.Add(new MedicalOperation
            {
                ParamedicId = paramedicId,
                ReportId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await service.DeleteParamedicAsync(paramedicId);

            Assert.Null(await context.Paramedics.FindAsync(paramedicId));
            Assert.False(await context.MedicalOperations.AnyAsync(o => o.ParamedicId == paramedicId));
        }
    }
}