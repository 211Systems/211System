using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.DTOs;
using _211system.Services;
using CPR112.Models;

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

        [Fact]
        public async Task GetAllAsync_ReturnsAllEncs()
        {
            var context = GetInMemoryDbContext();
            var encService = new EncService(context);

            context.Encs.AddRange(
                new Enc { Id = Guid.NewGuid(), Name = "CPR A", Region = "Mazowieckie" },
                new Enc { Id = Guid.NewGuid(), Name = "CPR B", Region = "Śląskie" }
            );
            await context.SaveChangesAsync();

            var result = await encService.GetAllAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, e => e.Name == "CPR A");
            Assert.Contains(result, e => e.Name == "CPR B");
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemoves()
        {
            var context = GetInMemoryDbContext();
            var encService = new EncService(context);

            var id = Guid.NewGuid();
            context.Encs.Add(new Enc { Id = id, Name = "Do usuniecia", Region = "Test" });
            await context.SaveChangesAsync();

            var deleted = await encService.DeleteAsync(id);

            Assert.True(deleted);
            Assert.Null(await context.Encs.FindAsync(id));
        }

        [Fact]
        public async Task DeleteAsync_UnknownId_ReturnsFalse()
        {
            var context = GetInMemoryDbContext();
            var encService = new EncService(context);

            var deleted = await encService.DeleteAsync(Guid.NewGuid());

            Assert.False(deleted);
        }
    }
}