using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models.Services;
using CPR112.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace _211system.Tests
{
    public class AttachmentServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        private static void EnsureAttachmentsFolder()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");
            Directory.CreateDirectory(dir);
        }

        private static IFormFile CreateFormFile(string fileName, byte[] content)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns((long)content.Length);
            mock.Setup(f => f.ContentType).Returns("image/jpeg");
            mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, ct) =>
                {
                    target.Write(content, 0, content.Length);
                    return Task.CompletedTask;
                });
            return mock.Object;
        }

        [Fact]
        public async Task UploadFileAsync_LinksToIncident()
        {
            EnsureAttachmentsFolder();
            var context = GetInMemoryDbContext();
            var service = new AttachmentService(context);
            var incidentId = Guid.NewGuid();
            var file = CreateFormFile("zdjecie.jpg", Encoding.UTF8.GetBytes("test"));

            var result = await service.UploadFileAsync(file, incidentId);

            Assert.Equal(incidentId, result.IncidentId);
            Assert.StartsWith("/attachments/", result.PathToFile);
            Assert.Single(context.Attachments.Where(a => a.IncidentId == incidentId));
        }

        [Fact]
        public async Task UploadFileAsync_NullFile_Throws()
        {
            var context = GetInMemoryDbContext();
            var service = new AttachmentService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadFileAsync(null!, Guid.NewGuid()));
        }
    }
}