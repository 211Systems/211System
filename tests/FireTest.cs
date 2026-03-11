using _211system.Data;
using _211system.Models.Dtos.Fire;
using _211system.Models.Services;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task CreateDepartmentAsync_ShouldAddFireDepartmentToDatabase()
    {
        var dbContext = await GetDatabaseContext();
        var fireService = new FireService(dbContext);
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
        var fireService = new FireService(dbContext);

        var departmentDto = new CreateFDepartmentDto { Name = "OSP Test", Address = "Test", District = "Test" };
        var department = await fireService.CreateDepartmentAsync(departmentDto);

        var firemanDto = new CreateFiremanDto
        {
            Name = "Piotr",
            Surname = "Zalewski",
            BadgeNumber = "PSP-998",
            Rank = "Kapitan",
            FDepartmentId = department.FDepartmentId,
            FireAccountId = "test-fire-acc"
        };

        var result = await fireService.CreateFiremanAsync(firemanDto);

        Assert.NotNull(result);
        Assert.Equal("Piotr", result.Name);
        Assert.Equal(1, await dbContext.Firemen.CountAsync());
    }

    [Fact]
    public async Task CreateFiremanAsync_WithInvalidDepartment_ShouldThrowException()
    {
        var dbContext = await GetDatabaseContext();
        var fireService = new FireService(dbContext);

        var firemanDto = new CreateFiremanDto
        {
            Name = "Adam",
            Surname = "Brakujący",
            BadgeNumber = "PSP-000",
            Rank = "Strażak",
            FDepartmentId = Guid.NewGuid(),
            FireAccountId = "test-fire-acc-2"
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => fireService.CreateFiremanAsync(firemanDto));

        Assert.Equal("Remiza o podanym ID nie istnieje!", exception.Message);
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_ShouldReturnAllDepartmentsAsDtos()
    {
        var dbContext = await GetDatabaseContext();
        var fireService = new FireService(dbContext);

        await fireService.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 1", Address = "A1", District = "D1" });
        await fireService.CreateDepartmentAsync(new CreateFDepartmentDto { Name = "JRG 2", Address = "A2", District = "D2" });

        var result = await fireService.GetAllDepartmentsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.Name == "JRG 2");
    }
}