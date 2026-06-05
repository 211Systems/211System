using _211system.DTOs;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace tests;

public static class TestServiceMocks
{
    public static Mock<ITransportService> CreateTransportService()
    {
        var mock = new Mock<ITransportService>();
        mock.Setup(t => t.RecordAsync(It.IsAny<RecordTransportDto>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    public static Mock<IAttachmentService> CreateAttachmentService()
    {
        var mock = new Mock<IAttachmentService>();
        mock.Setup(a => a.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<Guid>()))
            .ReturnsAsync((IFormFile file, Guid incidentId) => new CPR112.Models.Attachment
            {
                Id = Guid.NewGuid(),
                PathToFile = "https://storage.test/incidents/test.jpg",
                FileName = file.FileName,
                ContentType = file.ContentType ?? "image/jpeg",
                FileSizeBytes = file.Length,
                IncidentId = incidentId
            });
        mock.Setup(a => a.CountByIncidentAsync(It.IsAny<Guid>())).ReturnsAsync(0);
        return mock;
    }
}
