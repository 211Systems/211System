using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.Models.Hospital;
using _211system.Services;

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
            var service = new MedicalService(context);
            var paramedicId = Guid.NewGuid();
            var reportId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Test",
                LastName = "Test",
                LicenseNumber = "123",
                Specialization = "Ratownik",
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
            var service = new MedicalService(context);
            var paramedicId = Guid.NewGuid();

            context.Paramedics.Add(new Paramedic
            {
                Id = paramedicId,
                Name = "Test",
                LastName = "Test",
                LicenseNumber = "123",
                Specialization = "Ratownik",
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
            var service = new MedicalService(context);
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
    }
}