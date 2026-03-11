using _211system.Data;
using _211system.Models.Dtos.Police;
using _211system.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace tests;

public class PoliceTest
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
    public async Task CreateDepartmentAsync_ShouldAddDepartmentToDatabase()
    {
        var dbContext = await GetDatabaseContext();
        var policeService = new PoliceService(dbContext);
        var dto = new CreatePDepartmentDto
        {
            Name = "Komenda Główna",
            Address = "ul. Puławska 1",
            District = "Mokotów"
        };

        var result = await policeService.CreateDepartmentAsync(dto);


        var departmentInDb = await dbContext.PoliceDepartments.FirstOrDefaultAsync(d => d.PDepartmentId == result.PDepartmentId);

        Assert.NotNull(result);
        Assert.NotNull(departmentInDb);
        Assert.Equal("Komenda Główna", departmentInDb.Name);
    }

    [Fact]
    public async Task CreatePolicemanAsync_WithValidDepartment_ShouldAddPoliceman()
    {
        var dbContext = await GetDatabaseContext();
        var policeService = new PoliceService(dbContext);

        var departmentDto = new CreatePDepartmentDto { Name = "KPP Test", Address = "Test", District = "Test" };
        var department = await policeService.CreateDepartmentAsync(departmentDto);

        var policemanDto = new CreatePolicemanDto
        {
            Name = "Jan",
            Surname = "Kowalski",
            BadgeNumber = "12345",
            Rank = "Sierżant",
            PDepartmentId = department.PDepartmentId,
            PoliceAccountId = "test-acc-1"
        };

        var result = await policeService.CreatePolicemanAsync(policemanDto);

        Assert.NotNull(result);
        Assert.Equal("Jan", result.Name);
        Assert.Equal(1, await dbContext.Policemen.CountAsync());
    }

    [Fact]
    public async Task CreatePolicemanAsync_WithInvalidDepartment_ShouldThrowException()
    {
        var dbContext = await GetDatabaseContext();
        var policeService = new PoliceService(dbContext);

        var policemanDto = new CreatePolicemanDto
        {
            Name = "Anna",
            Surname = "Nowak",
            BadgeNumber = "999",
            Rank = "Aspirant",
            PDepartmentId = Guid.NewGuid(),
            PoliceAccountId = "test-acc-2"
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => policeService.CreatePolicemanAsync(policemanDto));

        Assert.Equal("Komenda o podanym ID nie istnieje!", exception.Message);
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_ShouldReturnAllDepartmentsAsDtos()
    {
        var dbContext = await GetDatabaseContext();
        var policeService = new PoliceService(dbContext);

        await policeService.CreateDepartmentAsync(new CreatePDepartmentDto { Name = "K1", Address = "A1", District = "D1" });
        await policeService.CreateDepartmentAsync(new CreatePDepartmentDto { Name = "K2", Address = "A2", District = "D2" });

        var result = await policeService.GetAllDepartmentsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.Name == "K1");
    }
}