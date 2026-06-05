using System.Security.Claims;
using _211system.DTOs;
using _211system.Models;
using _211system.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace _211system.Tests.Services
{
    public class AuthServiceTests
    {
        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);
        }

        private void SetupJwt(Mock<IConfiguration> mockConfig)
        {
            mockConfig.Setup(c => c["Jwt:Key"]).Returns("SuperTajnyKluczDoSystemu211_Minimum16Znakow!!!");
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("211System");
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns("211SystemUsers");
        }

        [Fact]
        public async Task CreateTemporaryAccountAsync_ValidData_CreatesUserAndReturnsCredentials()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            string testEmail = "policjant@211.pl";
            string testRole = "Policjant";

            mockUserManager.Setup(x => x.FindByEmailAsync(testEmail)).ReturnsAsync((ApplicationUser)null);
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockRoleManager.Setup(x => x.RoleExistsAsync(testRole)).ReturnsAsync(true);
            mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), testRole))
                .ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var result = await authService.CreateTemporaryAccountAsync(testEmail, testRole);

            Assert.NotNull(result.AccountId);
            Assert.StartsWith("Temp", result.TemporaryPassword);
            mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), testRole), Times.Once);
        }

        [Fact]
        public async Task CreateTemporaryAccountAsync_WhenRoleMissing_Throws()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            string testEmail = "nowy@211.pl";
            string testRole = "NieistniejacaRola";

            mockUserManager.Setup(x => x.FindByEmailAsync(testEmail)).ReturnsAsync((ApplicationUser)null);
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockRoleManager.Setup(x => x.RoleExistsAsync(testRole)).ReturnsAsync(false);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                authService.CreateTemporaryAccountAsync(testEmail, testRole));

            Assert.Contains(testRole, ex.Message);
        }

        [Fact]
        public async Task CreateTemporaryAccountAsync_WhenUserCreateFails_Throws()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            string testEmail = "fail@211.pl";

            mockUserManager.Setup(x => x.FindByEmailAsync(testEmail)).ReturnsAsync((ApplicationUser)null);
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Haslo za slabe" }));

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                authService.CreateTemporaryAccountAsync(testEmail, "Policjant"));

            Assert.Contains("Błąd tworzenia konta", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var loginDto = new LoginDto { Email = "test@test.pl", Password = "ValidPassword123" };
            var fakeUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = loginDto.Email };

            mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(fakeUser);
            mockUserManager.Setup(x => x.IsLockedOutAsync(fakeUser)).ReturnsAsync(false);
            mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, loginDto.Password)).ReturnsAsync(true);
            mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(fakeUser)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.GetRolesAsync(fakeUser)).ReturnsAsync(new List<string> { "Admin" });
            SetupJwt(mockConfig);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var token = await authService.LoginAsync(loginDto);

            Assert.False(string.IsNullOrEmpty(token));
            Assert.Equal(3, token.Split('.').Length);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var loginDto = new LoginDto { Email = "test@test.pl", Password = "WrongPassword" };
            var fakeUser = new ApplicationUser { Id = "123", Email = loginDto.Email, AccessFailedCount = 0 };

            mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(fakeUser);
            mockUserManager.Setup(x => x.IsLockedOutAsync(fakeUser)).ReturnsAsync(false);
            mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, loginDto.Password)).ReturnsAsync(false);
            mockUserManager.Setup(x => x.AccessFailedAsync(fakeUser)).ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorized()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var loginDto = new LoginDto { Email = "brak@211.pl", Password = "123" };

            mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((ApplicationUser)null);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidEmail_ReturnsJwt()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var email = "refresh@211.pl";
            var user = new ApplicationUser { Id = "1", Email = email };

            mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            SetupJwt(mockConfig);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var token = await authService.RefreshTokenAsync(email);

            Assert.False(string.IsNullOrEmpty(token));
            Assert.Equal(3, token.Split('.').Length);
        }

        [Fact]
        public async Task RefreshTokenAsync_UnknownEmail_Throws()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            mockUserManager.Setup(x => x.FindByEmailAsync("nie_ma@211.pl")).ReturnsAsync((ApplicationUser)null);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await Assert.ThrowsAsync<Exception>(() => authService.RefreshTokenAsync("nie_ma@211.pl"));
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidOldPassword_Updates()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var dto = new ChangePasswordDto
            {
                Email = "zmiana@211.pl",
                OldPassword = "Stare123!",
                NewPassword = "Nowe456!"
            };
            var user = new ApplicationUser { Id = "2", Email = dto.Email };

            mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await authService.ChangePasswordAsync(dto);

            mockUserManager.Verify(x => x.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongOldPassword_Throws()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var dto = new ChangePasswordDto
            {
                Email = "zle@211.pl",
                OldPassword = "Zle",
                NewPassword = "Nowe"
            };
            var user = new ApplicationUser { Id = "3", Email = dto.Email };

            mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Zle haslo" }));

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var ex = await Assert.ThrowsAsync<Exception>(() => authService.ChangePasswordAsync(dto));

            Assert.Contains("Błąd zmiany hasła", ex.Message);
        }

        [Fact]
        public async Task IsAccountLockedAsync_WhenLocked_ReturnsTrue()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var email = "locked@211.pl";
            var user = new ApplicationUser { Id = "4", Email = email };

            mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var result = await authService.IsAccountLockedAsync(email);

            Assert.True(result);
        }

        [Fact]
        public async Task LockAccountAsync_SetsLockoutEnd()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var email = "lock@211.pl";
            var user = new ApplicationUser { Id = "5", Email = email };

            mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
                .ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await authService.LockAccountAsync(email);

            mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        }

        [Fact]
        public async Task UnlockAccountAsync_ReturnsNewTempPassword()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var email = "unlock@211.pl";
            var user = new ApplicationUser { Id = "6", Email = email };

            mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.AddPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var tempPassword = await authService.UnlockAccountAsync(email);

            Assert.StartsWith("Temp", tempPassword);
            mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
            mockUserManager.Verify(x => x.AddPasswordAsync(user, It.Is<string>(p => p.StartsWith("Temp"))), Times.Once);
        }
    }
}