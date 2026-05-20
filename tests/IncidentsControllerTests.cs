using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using _211system.Controllers;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Services;
using _211system.Models.Interfaces;
using CPR112.Models;
using _211system.Models;

namespace _211system.Tests
{
    public class IncidentsControllerTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        private void SetupControllerUser(IncidentsController controller, string identityId, string role = "Dyspozytor112")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, identityId),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateIncident_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);

            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 3,
                IncidentTypeId = 1
            };

            serviceMock.Setup(s => s.CreateIncidentAsync(It.IsAny<CreateIncidentDto>()))
                       .ReturnsAsync(new IncidentDto { Description = "Test" });

            var result = await controller.CreateIncident(dto, null);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetIncidentById_Success_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            var id = Guid.NewGuid();
            
            serviceMock.Setup(s => s.GetIncidentByIdAsync(id)).ReturnsAsync(new IncidentDto { Id = id });

            var result = await controller.GetIncidentById(id);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetIncidentById_NotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            var id = Guid.NewGuid();

            serviceMock.Setup(s => s.GetIncidentByIdAsync(id)).ThrowsAsync(new ArgumentException("Not found"));

            var result = await controller.GetIncidentById(id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task ChangeStatus_Unauthorized_WhenNoIdentityIdInToken()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto(), null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_WhenOperatorNotInDbButAuthenticated()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            
            SetupControllerUser(controller, "admin-identity-id", "Admin112");

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto { NewStatus = "W toku" }, null);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_ServiceNotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto();
            serviceMock.Setup(s => s.ChangeIncidentStatusAsync(incidentId, operatorId, dto)).ThrowsAsync(new ArgumentException("Incident not found"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_InvalidOperation_ShouldReturn400()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto();
            serviceMock.Setup(s => s.ChangeIncidentStatusAsync(incidentId, operatorId, dto)).ThrowsAsync(new InvalidOperationException("Same status"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_ShouldReturn204()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var blobMock = new Mock<IBlobStorageService>();
            var weatherMock = new Mock<IWeatherService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context, blobMock.Object, weatherMock.Object);
            SetupControllerUser(controller, identityId);

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto { NewStatus = "W toku" }, null);

            Assert.IsType<NoContentResult>(result);
        }
    }
}