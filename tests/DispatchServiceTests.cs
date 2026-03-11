using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.DTOs;
using _211system.Services;
using Police;
using FireDepartment;

namespace _211system.Tests
{
    public class DispatchServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }


        [Fact]
        public async Task StartPolice_WhenUnitIsAvailable_ShouldCreateOperation()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var dto = new StartPoliceOperationDto { PDepartmentId = Guid.NewGuid(), IncidentId = Guid.NewGuid() };

            var id = await service.StartPoliceOperationAsync(dto);

            var operation = await context.PoliceOperations.FindAsync(id);
            Assert.NotNull(operation);
            Assert.Null(operation!.EndTime);
        }

        [Fact]
        public async Task StartPolice_WhenUnitIsBusy_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var busyId = Guid.NewGuid();

            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = busyId, EndTime = null });
            await context.SaveChangesAsync();

            var dto = new StartPoliceOperationDto { PDepartmentId = busyId };
            var exception = await Assert.ThrowsAsync<Exception>(() => service.StartPoliceOperationAsync(dto));
            Assert.Equal("Ta jednostka policji jest już w akcji!", exception.Message);
        }

        [Fact]
        public async Task EndPolice_WhenOperationExists_ShouldSetEndTime()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var operationId = Guid.NewGuid();

            context.PoliceOperations.Add(new PoliceOperation { Id = operationId, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = null });
            await context.SaveChangesAsync();

            await service.EndPoliceOperationAsync(operationId);
            var completed = await context.PoliceOperations.FindAsync(operationId);
            Assert.NotNull(completed!.EndTime);
        }

        [Fact]
        public async Task EndPolice_WhenNotFound_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var exception = await Assert.ThrowsAsync<Exception>(() => service.EndPoliceOperationAsync(Guid.NewGuid()));
            Assert.Equal("Nie znaleziono operacji.", exception.Message);
        }

        [Fact]
        public async Task EndPolice_WhenAlreadyEnded_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var operationId = Guid.NewGuid();

            context.PoliceOperations.Add(new PoliceOperation { Id = operationId, EndTime = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<Exception>(() => service.EndPoliceOperationAsync(operationId));
            Assert.Equal("Ta operacja już się zakończyła.", exception.Message);
        }


        [Fact]
        public async Task StartFire_WhenUnitIsAvailable_ShouldCreateOperation()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var dto = new StartFireOperationDto { FDepartmentId = Guid.NewGuid(), IncidentId = Guid.NewGuid() };

            var id = await service.StartFireOperationAsync(dto);
            var operation = await context.FireOperations.FindAsync(id);
            Assert.Null(operation!.EndTime);
        }

        [Fact]
        public async Task StartFire_WhenUnitIsBusy_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var busyId = Guid.NewGuid();

            context.FireOperations.Add(new FireDepartmentOperation { FDepartmentId = busyId, EndTime = null });
            await context.SaveChangesAsync();

            var dto = new StartFireOperationDto { FDepartmentId = busyId };
            var exception = await Assert.ThrowsAsync<Exception>(() => service.StartFireOperationAsync(dto));
            Assert.Equal("Ta jednostka straży jest już w akcji (brak czasu powrotu)!", exception.Message);
        }

        [Fact]
        public async Task EndFire_WhenOperationExists_ShouldSetEndTime()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var operationId = Guid.NewGuid();

            context.FireOperations.Add(new FireDepartmentOperation { Id = operationId, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = null });
            await context.SaveChangesAsync();

            await service.EndFireOperationAsync(operationId);
            var completed = await context.FireOperations.FindAsync(operationId);
            Assert.NotNull(completed!.EndTime);
        }

        [Fact]
        public async Task EndFire_WhenNotFound_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var exception = await Assert.ThrowsAsync<Exception>(() => service.EndFireOperationAsync(Guid.NewGuid()));
            Assert.Equal("Nie znaleziono operacji.", exception.Message);
        }

        [Fact]
        public async Task EndFire_WhenAlreadyEnded_ShouldThrowException()
        {
            var context = GetInMemoryDbContext();
            var service = new DispatchService(context);
            var operationId = Guid.NewGuid();

            context.FireOperations.Add(new FireDepartmentOperation { Id = operationId, EndTime = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<Exception>(() => service.EndFireOperationAsync(operationId));
            Assert.Equal("Ta operacja już się zakończyła.", exception.Message);
        }
    }
}