using _211system.Data;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace tests;

public class FireServiceTests
{
    private async Task<_211DbContext> GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<_211DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var databaseContext = new _211DbContext(options);
        databaseContext.Database.EnsureCreated();
        return databaseContext;
    }

    private Mock<IAuthService> GetMockAuthService()
    {
        var mock = new Mock<IAuthService>();

        mock.Setup(s => s.CreateTemporaryAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("mock-account-id-123", "Temp9999"));
        return mock;
    }

    [Fact]
    public async Task CreateDepartmentAsync_ShouldAddFireDepartmentToDatabase()
    {
        var dbContext = await GetDatabaseContext();
        var authMock = GetMockAuthService();
        var fireService = new FireService(dbContext, authMock.Object);

        var dto = new CreateFDepartmentDto
        {
            Name = "JRG 1",
            Address = "ul. Strażacka 998",
            District = "Centrum"
        };

        var result = await fireService.CreateDepartmentAsync(dto);
        var departmentInDb = await dbContext.FireDepartments.FirstOrDefaultAsync(d => d.FDepartmentId == result.FDepartmentId);

        Assert.NotNull(result);
        Assert.NotNull(departmentInDb);
        Assert.Equal("JRG 1", departmentInDb.Name);
    }

    [Fact]
    public async Task CreateFiremanAsync_WithValidDepartment_ShouldAddFireman()
    {
        var dbContext = await GetDatabaseContext();
        var authMock = GetMockAuthService();
        var fireService = new FireService(dbContext, authMock.Object);

        var departmentDto = new CreateFDepartmentDto { Name = "OSP Test", Address = "Test", District = "Test" };
        var department = await fireService.CreateDepartmentAsync(departmentDto);

        var firemanDto = new CreateFiremanDto
        {
            Name = "Piotr",
            Surname = "Zalewski",
            BadgeNumber = "PSP-998",
            Rank = "Kapitan",
            FDepartmentId = department.FDepartmentId,
            Email = "piotr@straz.pl" 
        };

        var result = await fireService.CreateFiremanAsync(firemanDto);

        Assert.NotNull(result);
        Assert.Equal("piotr@straz.pl", result.Email);
        Assert.Equal("Temp9999", result.TemporaryPassword);
        Assert.Equal(1, await dbContext.Firemen.CountAsync());
    }

    [Fact]
    public async Task CreateFiremanAsync_WithInvalidDepartment_ShouldThrowException()
    {
        var dbContext = await GetDatabaseContext();
        var authMock = GetMockAuthService();
        var fireService = new FireService(dbContext, authMock.Object);

        var firemanDto = new CreateFiremanDto
        {
            Name = "Adam",
            Surname = "Brakujący",
            BadgeNumber = "PSP-000",
            Rank = "Strazak",
            FDepartmentId = Guid.NewGuid(),
            Email = "adam@straz.pl"
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => fireService.CreateFiremanAsync(firemanDto));
        Assert.Equal("Remiza o podanym ID nie istnieje!", exception.Message);
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_ShouldReturnAllDepartmentsAsDtos()
    {
        var dbContext = await GetDatabaseContext();
        var authMock = GetMockAuthService();
        var fireService = new FireService(dbContext, authMock.Object);

        await fireService.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 1", Address = "A1", District = "D1" });
        await fireService.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 2", Address = "A2", District = "D2" });

        var result = await fireService.GetAllDepartmentsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.Name == "JRG 2");
    }
}