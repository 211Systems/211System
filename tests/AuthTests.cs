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

        [Fact]
        public async Task CreateTemporaryAccountAsync_ValidData_CreatesUserAndReturnsCredentials()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            string testEmail = "policjant@211.pl";
            string testRole = "Policjant";

            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockRoleManager.Setup(x => x.RoleExistsAsync(testRole))
                .ReturnsAsync(true);

            mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), testRole))
                .ReturnsAsync(IdentityResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var result = await authService.CreateTemporaryAccountAsync(testEmail, testRole);

            Assert.NotNull(result.AccountId);
            Assert.StartsWith("Temp", result.TemporaryPassword);

            mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), testRole), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var loginDto = new LoginDto { Email = "test@test.pl", Password = "ValidPassword123" };
            var fakeUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = loginDto.Email };

            mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(fakeUser);

            mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, loginDto.Password))
                .ReturnsAsync(true);

            mockUserManager.Setup(x => x.GetRolesAsync(fakeUser))
                .ReturnsAsync(new List<string> { "Admin" });

            mockConfig.Setup(c => c["Jwt:Key"]).Returns("SuperTajnyKluczDoSystemu211_Minimum16Znakow!!!");
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("211System");
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns("211SystemUsers");

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            var token = await authService.LoginAsync(loginDto);

            Assert.False(string.IsNullOrEmpty(token));
            var tokenParts = token.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var mockConfig = new Mock<IConfiguration>();

            var loginDto = new LoginDto { Email = "test@test.pl", Password = "WrongPassword" };
            var fakeUser = new ApplicationUser { Id = "123", Email = loginDto.Email };

            mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(fakeUser);

            mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, loginDto.Password))
                .ReturnsAsync(false);

            var authService = new AuthService(mockUserManager.Object, mockRoleManager.Object, mockConfig.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(loginDto));
        }
    }
}