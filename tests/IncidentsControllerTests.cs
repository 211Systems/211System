using _211system.Controllers;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Models;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using _211system.Services;
using CPR112.Models;
using tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace _211system.Tests
{
    public class IncidentsControllerTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new _211DbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private IncidentsController CreateController(
            _211DbContext context,
            Mock<IIncidentService>? serviceMock = null,
            Mock<IBlobStorageService>? blobMock = null,
            Mock<IWeatherService>? weatherMock = null,
            Mock<IAttachmentService>? attachmentMock = null)
        {
            serviceMock ??= new Mock<IIncidentService>();
            blobMock ??= new Mock<IBlobStorageService>();
            weatherMock ??= new Mock<IWeatherService>();
            attachmentMock ??= TestServiceMocks.CreateAttachmentService();

            return new IncidentsController(
                serviceMock.Object,
                context,
                blobMock.Object,
                weatherMock.Object,
                attachmentMock.Object);
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

        private void SetupChangeStatusSuccess(Mock<IIncidentService> serviceMock)
        {
            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ChangeIncidentStatusDto>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task CreateIncident_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);

            SetupControllerUser(controller, "test-user-id");

            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 3,
                IncidentTypeId = 1
            };

            serviceMock
                .Setup(s => s.CreateIncidentAsync(It.IsAny<CreateIncidentDto>()))
                .ReturnsAsync(new IncidentDto { Id = Guid.NewGuid(), Description = "Test" });

            var result = await controller.CreateIncident(dto, null, null);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateIncident_WithTooManyPhotos_ShouldReturnBadRequest()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            SetupControllerUser(controller, "test-user-id");

            var dto = new CreateIncidentDto
            {
                Description = "Test",
                SeverityLevelId = 3,
                IncidentTypeId = 1
            };

            var files = Enumerable.Range(0, 11)
                .Select(_ => CreateMockFormFile("f.jpg"))
                .ToList();

            var result = await controller.CreateIncident(dto, null, files);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateIncident_WithAttachments_ShouldUploadAndSetPhotoUrl()
        {
            var context = GetInMemoryDbContext();
            var incidentId = Guid.NewGuid();
            var serviceMock = new Mock<IIncidentService>();
            var attachmentMock = TestServiceMocks.CreateAttachmentService();
            var blobMock = new Mock<IBlobStorageService>();
            blobMock.Setup(b => b.GetSecureFileUrl(It.IsAny<string>(), "incidents", It.IsAny<int>()))
                .Returns("https://secure.test/photo.jpg");

            var controller = CreateController(context, serviceMock, blobMock, null, attachmentMock);
            SetupControllerUser(controller, "test-user-id");

            serviceMock
                .Setup(s => s.CreateIncidentAsync(It.IsAny<CreateIncidentDto>()))
                .ReturnsAsync(new IncidentDto { Id = incidentId, Description = "Z foto" });

            context.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "ZGL/1",
                Description = "Z foto",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Status = "Nowe"
            });
            await context.SaveChangesAsync();

            var dto = new CreateIncidentDto
            {
                Description = "Z foto",
                SeverityLevelId = 3,
                IncidentTypeId = 1
            };

            var result = await controller.CreateIncident(dto, null, new List<IFormFile> { CreateMockFormFile("zdjecie.jpg") });

            Assert.IsType<OkObjectResult>(result.Result);
            attachmentMock.Verify(a => a.UploadFileAsync(It.IsAny<IFormFile>(), incidentId), Times.Once);
        }

        [Fact]
        public async Task GetAllIncidents_ShouldIncludeAttachmentCount()
        {
            var context = GetInMemoryDbContext();
            var incidentId = Guid.NewGuid();
            context.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "ZGL/2",
                Description = "Test",
                SeverityLevelId = 1,
                IncidentTypeId = 1,
                ReportDate = DateTime.UtcNow,
                Status = "Nowe",
                Latitude = 52.0,
                Longitude = 21.0
            });
            context.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                PathToFile = "https://blob.test/a.jpg",
                FileName = "a.jpg",
                ContentType = "image/jpeg",
                FileSizeBytes = 100,
                UploadedAt = DateTime.UtcNow
            });
            context.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                PathToFile = "https://blob.test/b.jpg",
                FileName = "b.jpg",
                ContentType = "image/jpeg",
                FileSizeBytes = 200,
                UploadedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            Assert.Equal(1, await context.Incidents.CountAsync());
            Assert.Equal(2, await context.Attachments.CountAsync());

            var blobMock = new Mock<IBlobStorageService>();
            blobMock.Setup(b => b.GetSecureFileUrl(It.IsAny<string>(), "incidents", It.IsAny<int>()))
                .Returns("https://secure.test/file.jpg");
            var controller = CreateController(context, blobMock: blobMock);
            SetupControllerUser(controller, "test-user-id");

            var result = await controller.GetAllIncidents();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<IncidentDto>>(ok.Value);
            var inc = Assert.Single(list);
            Assert.Equal(2, inc.AttachmentCount);
        }

        private static IFormFile CreateMockFormFile(string fileName)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(1024);
            mock.Setup(f => f.ContentType).Returns("image/jpeg");
            return mock.Object;
        }

        [Fact]
        public async Task GetIncidentById_Success_ShouldReturnOk()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);
            var id = Guid.NewGuid();

            serviceMock
                .Setup(s => s.GetIncidentByIdAsync(id))
                .ReturnsAsync(new IncidentDto { Id = id });

            var result = await controller.GetIncidentById(id);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetIncidentById_NotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);
            var id = Guid.NewGuid();

            serviceMock
                .Setup(s => s.GetIncidentByIdAsync(id))
                .ThrowsAsync(new ArgumentException("Not found"));

            var result = await controller.GetIncidentById(id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task ChangeStatus_Unauthorized_WhenNoIdentityIdInToken()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            var result = await controller.ChangeStatus(Guid.NewGuid(), new ChangeIncidentStatusDto(), null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_WhenOperatorNotInDbButAuthenticated()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var controller = CreateController(context, serviceMock);

            SetupControllerUser(controller, "admin-identity-id", "Admin112");
            SetupChangeStatusSuccess(serviceMock);

            var result = await controller.ChangeStatus(
                Guid.NewGuid(),
                new ChangeIncidentStatusDto { NewStatus = "W toku" },
                null);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_ServiceNotFound_ShouldReturn404()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto { NewStatus = "W toku" };

            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    incidentId,
                    operatorId,
                    It.IsAny<ChangeIncidentStatusDto>()))
                .ThrowsAsync(new ArgumentException("Incident not found"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_InvalidOperation_ShouldReturn400()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);

            var incidentId = Guid.NewGuid();
            var dto = new ChangeIncidentStatusDto { NewStatus = "W toku" };

            serviceMock
                .Setup(s => s.ChangeIncidentStatusAsync(
                    incidentId,
                    operatorId,
                    It.IsAny<ChangeIncidentStatusDto>()))
                .ThrowsAsync(new InvalidOperationException("Same status"));

            var result = await controller.ChangeStatus(incidentId, dto, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangeStatus_Success_ShouldReturn204()
        {
            var context = GetInMemoryDbContext();
            var serviceMock = new Mock<IIncidentService>();
            var identityId = "id-123";
            var operatorId = Guid.NewGuid();

            context.Operators112.Add(new Operator112
            {
                Id = operatorId,
                OpAccountId = identityId,
                FirstName = "A",
                LastName = "B",
                StationNumber = "1",
                EncId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, serviceMock);
            SetupControllerUser(controller, identityId);
            SetupChangeStatusSuccess(serviceMock);

            var result = await controller.ChangeStatus(
                Guid.NewGuid(),
                new ChangeIncidentStatusDto { NewStatus = "W toku" },
                null);

            Assert.IsType<NoContentResult>(result);
        }
    }
}