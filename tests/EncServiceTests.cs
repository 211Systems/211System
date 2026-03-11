using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.DTOs;
using _211system.Services;

namespace _211system.Tests
{
    public class EncServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            return new _211DbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddEncToDatabase_AndReturnDto()
        {

            var context = GetInMemoryDbContext(); 
            
            var encService = new EncService(context); 
            
            var createDto = new CreateEncDto 
            { 
                Name = "Główne CPR Testowe", 
                Region = "Mazowieckie" 
            };

            var result = await encService.CreateAsync(createDto);

            Assert.NotNull(result);
            Assert.Equal("Główne CPR Testowe", result.Name);
            Assert.Equal("Mazowieckie", result.Region);
            Assert.NotEqual(Guid.Empty, result.Id);
            
            var encInDb = await context.Encs.FirstOrDefaultAsync(e => e.Id == result.Id);
            
            Assert.NotNull(encInDb);
            Assert.Equal("Główne CPR Testowe", encInDb.Name);
        }
    }
}