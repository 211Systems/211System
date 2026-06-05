using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models.Interfaces;
using _211system.Models.Services;
using _211system.Services;
using CPR112.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace tests;

    public class AttachmentServiceTests
    {
    private _211DbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

    private static Mock<IFormFile> CreateFile(string name, long size = 1024)
        {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.ContentType).Returns("image/jpeg");
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return mock;
        }

    [Fact]
    public async Task UploadFileAsync_Should_Save_Attachment_To_Database()
        {
        var context = GetContext();
        var blobMock = new Mock<IBlobStorageService>();
        blobMock.Setup(b => b.UploadAsync(It.IsAny<IFormFile>(), "incidents"))
            .ReturnsAsync("https://blob.test/incidents/abc.jpg");

        var service = new AttachmentService(context, blobMock.Object);
        var incidentId = Guid.NewGuid();

        var result = await service.UploadFileAsync(CreateFile("test.jpg").Object, incidentId);

        Assert.Equal("test.jpg", result.FileName);
        Assert.Equal(incidentId, result.IncidentId);
        Assert.Equal(1, await context.Attachments.CountAsync());
    }

    [Fact]
    public async Task UploadFileAsync_WhenLimitExceeded_ShouldThrow()
                {
        var context = GetContext();
        var incidentId = Guid.NewGuid();
        for (int i = 0; i < AttachmentService.MaxAttachmentsPerIncident; i++)
        {
            context.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                PathToFile = $"https://blob.test/{i}.jpg",
                FileName = $"{i}.jpg",
                ContentType = "image/jpeg",
                FileSizeBytes = 100,
                UploadedAt = DateTime.UtcNow
                });
        }
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var service = new AttachmentService(context, blobMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadFileAsync(CreateFile("extra.jpg").Object, incidentId));
        }

        [Fact]
    public async Task GetByIncidentAsync_Should_Return_All_Attachments()
        {
        var context = GetContext();
            var incidentId = Guid.NewGuid();
        context.Attachments.AddRange(
            new Attachment { Id = Guid.NewGuid(), IncidentId = incidentId, PathToFile = "a", FileName = "a.jpg", ContentType = "image/jpeg", FileSizeBytes = 1, UploadedAt = DateTime.UtcNow },
            new Attachment { Id = Guid.NewGuid(), IncidentId = incidentId, PathToFile = "b", FileName = "b.jpg", ContentType = "image/jpeg", FileSizeBytes = 2, UploadedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new AttachmentService(context, new Mock<IBlobStorageService>().Object);
        var list = await service.GetByIncidentAsync(incidentId);

        Assert.Equal(2, list.Count);
        }

        [Fact]
    public async Task DeleteAsync_Should_Remove_Attachment()
    {
        var context = GetContext();
        var attId = Guid.NewGuid();
        context.Attachments.Add(new Attachment
        {
            Id = attId,
            IncidentId = Guid.NewGuid(),
            PathToFile = "https://blob.test/x.jpg",
            FileName = "x.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1,
            UploadedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        blobMock.Setup(b => b.DeleteAsync(It.IsAny<string>(), "incidents")).ReturnsAsync(true);

        var service = new AttachmentService(context, blobMock.Object);
        var deleted = await service.DeleteAsync(attId);

        Assert.True(deleted);
        Assert.Equal(0, await context.Attachments.CountAsync());
    }
}