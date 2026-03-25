using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using _211system.Data;
using _211system.DTOs;
using _211system.Models.Interfaces;
using _211system.Services;
using CPR112.Models;
using _211system.Models;
using Microsoft.AspNetCore.Identity;

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

        private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task CreateAsync_WhenEncExists_ShouldAddOperatorAndAccount()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();
            
            var fakeAccountId = "test-identity-id";
            var fakePassword = "TempPassword123";
            
            authMock.Setup(a => a.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((fakeAccountId, fakePassword));

            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);
            
            var validEncId = Guid.NewGuid();
            context.Encs.Add(new Enc { Id = validEncId, Name = "CPR Warszawa", Region = "Mazowieckie" });
            await context.SaveChangesAsync();

            var createDto = new CreateOperatorDto
            {
                FirstName = "Anna",
                LastName = "Nowak",
                Email = "anna@112.pl",
                Rank = "Dyspozytor112",
                StationNumber = "Stanowisko-5",
                EncId = validEncId
            };


            var (result, tempPassword) = await operatorService.CreateAsync(createDto);

            Assert.NotNull(result);
            Assert.Equal(fakePassword, tempPassword);
            Assert.Equal(fakeAccountId, result.OpAccountId);

            var opInDb = await context.Operators112.FirstOrDefaultAsync(o => o.Id == result.Id);
            Assert.NotNull(opInDb);
            Assert.Equal(fakeAccountId, opInDb.OpAccountId);
        }
    }
}