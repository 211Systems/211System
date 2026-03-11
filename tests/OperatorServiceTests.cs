using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.DTOs;
using _211system.Services;
using CPR112.Models;

namespace _211system.Tests
{
    public class OperatorServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            return new _211DbContext(options);
        }

        [Fact]
        public async Task CreateAsync_WhenEncExists_ShouldAddOperator()
        {
            var context = GetInMemoryDbContext();
            
            var validEncId = Guid.NewGuid();
            context.Encs.Add(new Enc { Id = validEncId, Name = "CPR Warszawa", Region = "Mazowieckie" });
            await context.SaveChangesAsync();

            var operatorService = new OperatorService(context);
            var createDto = new CreateOperatorDto
            {
                FirstName = "Anna",
                LastName = "Nowak",
                StationNumber = "Stanowisko-5",
                OpAccountId = "konto-testowe",
                EncId = validEncId
            };

            var result = await operatorService.CreateAsync(createDto);
            Assert.NotNull(result);
            Assert.Equal("Anna", result.FirstName);

            var opInDb = await context.Operators112.FirstOrDefaultAsync(o => o.Id == result.Id);
            Assert.NotNull(opInDb);
            Assert.Equal(validEncId, opInDb.EncId);
        }

        [Fact]
        public async Task CreateAsync_WhenEncDoesNotExist_ShouldThrowException()
        {

            var context = GetInMemoryDbContext();
            var operatorService = new OperatorService(context);
            
            var createDto = new CreateOperatorDto
            {
                FirstName = "Anna",
                LastName = "Nowak",
                StationNumber = "Stanowisko-5",
                OpAccountId = "konto-testowe",
                EncId = Guid.NewGuid()
            };

            var exception = await Assert.ThrowsAsync<Exception>(() => operatorService.CreateAsync(createDto));

            Assert.Equal("Podana placówka CPR nie istnieje!", exception.Message);
        }
    }
}