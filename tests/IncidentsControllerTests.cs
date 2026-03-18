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
using CPR112.Models;

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

        private void SetupControllerUser(IncidentsController controller, string identityId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, identityId),
                new Claim(ClaimTypes.Role, "Dyspozytor112")
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
            var controller = new IncidentsController(serviceMock.Object, context);
            var dto = new CreateIncidentDto { Description = "Test" };
            
            serviceMock.Setup(s => s.CreateIncidentAsync(dto)).ReturnsAsync(new IncidentDto());

            var result = await controller.CreateIncident(dto);

            Assert.IsType<OkObjectResult>(result.Result);
        }


        [Fact]
        public async Task GetIncidentById_Success_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = new IncidentsController(serviceMock.Object, context);
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
            var controller = new IncidentsController(serviceMock.Object, context);
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
            var controller = new IncidentsController(serviceMock.Object, context);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto());

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Forbidden_WhenOperatorNotInDb()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = new IncidentsController(serviceMock.Object, context);
            SetupControllerUser(controller, "non-existent-id");

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto());

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_ServiceNotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto();
            serviceMock.Setup(s => s.ChangeIncidentStatusAsync(incidentId, operatorId, dto)).ThrowsAsync(new ArgumentException("Incident not found"));

            var result = await controller.ChangeStatus(incidentId, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_InvalidOperation_ShouldReturn400()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto();
            serviceMock.Setup(s => s.ChangeIncidentStatusAsync(incidentId, operatorId, dto)).ThrowsAsync(new InvalidOperationException("Same status"));

            var result = await controller.ChangeStatus(incidentId, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_ShouldReturn204()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();
            
            context.Operators112.Add(new Operator112 { Id = operatorId, OpAccountId = identityId, FirstName="A", LastName="B", StationNumber="1", EncId=Guid.NewGuid() });
            await context.SaveChangesAsync();

            var controller = new IncidentsController(serviceMock.Object, context);
            SetupControllerUser(controller, identityId);

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto { NewStatus = "W toku" });

            Assert.IsType<NoContentResult>(result);
        }
    }
}