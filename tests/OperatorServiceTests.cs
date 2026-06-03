using System;
using System.Linq;
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

        [Fact]
        public async Task CreateAsync_WhenEncMissing_Throws()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();
            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var createDto = new CreateOperatorDto
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Email = "jan@112.pl",
                Rank = "Dyspozytor112",
                StationNumber = "1",
                EncId = Guid.NewGuid()
            };

            var ex = await Assert.ThrowsAsync<Exception>(() => operatorService.CreateAsync(createDto));

            Assert.Contains("placówka CPR nie istnieje", ex.Message);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOperatorsWithEncName()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();
            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var encId = Guid.NewGuid();
            context.Encs.Add(new Enc { Id = encId, Name = "CPR Krakow", Region = "Malopolskie" });

            var accountId = Guid.NewGuid().ToString();
            context.Users.Add(new ApplicationUser { Id = accountId, Email = "op@112.pl", UserName = "op@112.pl" });

            context.Operators112.Add(new Operator112
            {
                Id = Guid.NewGuid(),
                FirstName = "Tomek",
                LastName = "Test",
                StationNumber = "3",
                Rank = OperatorRank.Dyspozytor112,
                OpAccountId = accountId,
                EncId = encId
            });
            await context.SaveChangesAsync();

            var result = await operatorService.GetAllAsync();

            Assert.Single(result);
            var op = result.First();
            Assert.Equal(encId, op.EncId);
            Assert.Equal("op@112.pl", op.Email);
            Assert.Equal("Tomek", op.FirstName);
        }

        [Fact]
        public async Task DeleteAsync_Existing_ReturnsTrue()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();

            var opId = Guid.NewGuid();
            var accountId = "acc-delete";
            context.Operators112.Add(new Operator112
            {
                Id = opId,
                FirstName = "Usun",
                LastName = "Mnie",
                StationNumber = "1",
                Rank = OperatorRank.Dyspozytor112,
                OpAccountId = accountId,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            userManagerMock.Setup(x => x.FindByIdAsync(accountId))
                .ReturnsAsync(new ApplicationUser { Id = accountId, Email = "del@112.pl" });
            userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var deleted = await operatorService.DeleteAsync(opId);

            Assert.True(deleted);
            Assert.Null(await context.Operators112.FindAsync(opId));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFalse()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();
            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var deleted = await operatorService.DeleteAsync(Guid.NewGuid());

            Assert.False(deleted);
        }

        [Fact]
        public async Task ChangeRankAsync_Valid_UpdatesRank()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();

            var opId = Guid.NewGuid();
            var accountId = "acc-rank";
            var user = new ApplicationUser { Id = accountId, Email = "rank@112.pl" };

            context.Operators112.Add(new Operator112
            {
                Id = opId,
                FirstName = "Rank",
                LastName = "Test",
                StationNumber = "2",
                Rank = OperatorRank.Dyspozytor112,
                OpAccountId = accountId,
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            userManagerMock.Setup(x => x.FindByIdAsync(accountId)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Dyspozytor112" });
            userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin112"))
                .ReturnsAsync(IdentityResult.Success);

            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var ok = await operatorService.ChangeRankAsync(opId, "Admin112");

            Assert.True(ok);
            var opInDb = await context.Operators112.FindAsync(opId);
            Assert.Equal(OperatorRank.Admin112, opInDb.Rank);
        }

        [Fact]
        public async Task ChangeRankAsync_NotFound_ReturnsFalse()
        {
            var context = GetInMemoryDbContext();
            var authMock = new Mock<IAuthService>();
            var userManagerMock = GetUserManagerMock();
            var operatorService = new OperatorService(context, authMock.Object, userManagerMock.Object);

            var ok = await operatorService.ChangeRankAsync(Guid.NewGuid(), "Admin112");

            Assert.False(ok);
        }
    }
}