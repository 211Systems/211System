using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.Services;
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

            context.PoliceDepartments.Add(new PDepartment { 
                PDepartmentId = availablePoliceId, 
                Name = "Policja Dostępna",
                Address = "Testowa 1",
                District = "Warszawa"
            });
            
            context.PoliceDepartments.Add(new PDepartment { 
                PDepartmentId = busyPoliceId, 
                Name = "Policja Zajęta",
                Address = "Testowa 2",
                District = "Warszawa"
            });
            
            context.FireDepartments.Add(new FDepartment { 
                FDepartmentId = availableFireId, 
                Name = "Straż Dostępna",
                Address = "Testowa 3",
                District = "Warszawa"
            });
            
            context.FireDepartments.Add(new FDepartment { 
                FDepartmentId = busyFireId, 
                Name = "Straż Zajęta",
                Address = "Testowa 4",
                District = "Warszawa"
            });
            
            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = availablePoliceId, EndTime = DateTime.UtcNow });
            
            context.PoliceOperations.Add(new PoliceOperation { PDepartmentId = busyPoliceId, EndTime = null, IncidentId = incidentId });

            context.FireOperations.Add(new FireDepartmentOperation { FDepartmentId = busyFireId, EndTime = null, IncidentId = incidentId });

            await context.SaveChangesAsync();

            var board = await service.GetReadinessBoardAsync();

            Assert.NotNull(board);
            Assert.Equal(4, board.Count);

            Assert.Equal("Dostępny", board[0].Status);
            Assert.Equal("Dostępny", board[1].Status);
            Assert.Equal("W akcji", board[2].Status);
            Assert.Equal("W akcji", board[3].Status);

            var busyPolice = board.First(b => b.DepartmentId == busyPoliceId);
            Assert.Equal("W akcji", busyPolice.Status);
            Assert.Equal("Policja", busyPolice.Type);
            Assert.Equal(incidentId, busyPolice.CurrentIncidentId);

            var availableFire = board.First(b => b.DepartmentId == availableFireId);
            Assert.Equal("Dostępny", availableFire.Status);
            Assert.Null(availableFire.CurrentIncidentId);
        }
    }
}