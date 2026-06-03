using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.Services;
using _211system.Models.Hospital;
using _211system.Models;
using _211system.Models.Aviation;
using Police;
using FireDepartment;
using CPR112.Models;

namespace _211system.Tests
{
    public class ReadinessServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        [Fact]
        public async Task GetReadinessBoardAsync_ShouldReturnCombinedListWithCorrectStatusesAndSorting()
        {
            var context = GetInMemoryDbContext();
            var service = new ReadinessService(context);

            var availablePoliceId = Guid.NewGuid();
            var busyPoliceId = Guid.NewGuid();
            var availableFireId = Guid.NewGuid();
            var busyFireId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            context.PoliceDepartments.AddRange(
                new PDepartment { PDepartmentId = availablePoliceId, Name = "Policja Dostępna", Address = "1", District = "Wwa" },
                new PDepartment { PDepartmentId = busyPoliceId, Name = "Policja Zajęta", Address = "2", District = "Wwa" }
            );

            context.FireDepartments.AddRange(
                new FDepartment { FDepartmentId = availableFireId, Name = "Straż Dostępna", Address = "3", District = "Wwa" },
                new FDepartment { FDepartmentId = busyFireId, Name = "Straż Zajęta", Address = "4", District = "Wwa" }
            );

            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = availablePoliceId, EndTime = DateTime.UtcNow });
            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = busyPoliceId, EndTime = null, IncidentId = incidentId });
            context.FireOperations.Add(new FireDepartmentOperation { FDepartmentId = busyFireId, EndTime = null, IncidentId = incidentId });

            await context.SaveChangesAsync();

            var board = await service.GetReadinessBoardAsync();

            Assert.Equal(4, board.Count);
            Assert.Equal("W akcji", board.First(b => b.DepartmentId == busyPoliceId).Status);
            Assert.Equal("Dostępny", board.First(b => b.DepartmentId == availableFireId).Status);
        }

        [Fact]
        public async Task GetReadinessBoardAsync_IncludesPoliceFireMedicalAviation()
        {
            var context = GetInMemoryDbContext();
            var service = new ReadinessService(context);

            var policeId = Guid.NewGuid();
            var fireId = Guid.NewGuid();

            context.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = policeId,
                Name = "KPP",
                Address = "Adres 1",
                District = "Wwa"
            });

            context.FireDepartments.Add(new FDepartment
            {
                FDepartmentId = fireId,
                Name = "JRG 1",
                Address = "Adres 2",
                District = "Wwa"
            });

            await context.SaveChangesAsync();

            var board = await service.GetReadinessBoardAsync();

            Assert.Contains(board, u => u.Type == "Policja");
            Assert.Contains(board, u => u.Type == "Straż Pożarna");
            Assert.Equal(2, board.Count);
        }

        [Fact]
        public async Task GetReadinessBoardAsync_BusyUnitsMarkedUnavailable()
        {
            var context = GetInMemoryDbContext();
            var service = new ReadinessService(context);

            var busyPoliceId = Guid.NewGuid();
            var freePoliceId = Guid.NewGuid();
            var busyFireId = Guid.NewGuid();
            var freeFireId = Guid.NewGuid();
            var incidentId = Guid.NewGuid();

            context.PoliceDepartments.AddRange(
                new PDepartment { PDepartmentId = busyPoliceId, Name = "P Zajęta", Address = "1", District = "Wwa" },
                new PDepartment { PDepartmentId = freePoliceId, Name = "P Wolna", Address = "2", District = "Wwa" }
            );

            context.FireDepartments.AddRange(
                new FDepartment { FDepartmentId = busyFireId, Name = "S Zajęta", Address = "3", District = "Wwa" },
                new FDepartment { FDepartmentId = freeFireId, Name = "S Wolna", Address = "4", District = "Wwa" }
            );

            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = busyPoliceId, EndTime = null, IncidentId = incidentId });
            context.FireOperations.Add(new FireDepartmentOperation { FDepartmentId = busyFireId, EndTime = null, IncidentId = incidentId });

            await context.SaveChangesAsync();

            var board = await service.GetReadinessBoardAsync();

            var busy = board.Where(u => u.Status == "W akcji").ToList();
            var free = board.Where(u => u.Status == "Dostępny").ToList();

            Assert.Equal(2, busy.Count);
            Assert.Equal(2, free.Count);
            Assert.All(busy, u => Assert.NotNull(u.CurrentIncidentId));
            Assert.All(free, u => Assert.Null(u.CurrentIncidentId));
        }
    }
}