using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _211system.Services;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace _211system.Tests
{
    public class BlobStorageServiceTests
    {
        private static IFormFile CreateFormFile(string fileName, byte[] content, string contentType = "application/pdf")
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns((long)content.Length);
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            return mock.Object;
        }

        private static BlobStorageService CreateServiceWithMocks(out Uri blobUri)
        {
            blobUri = new Uri("https://devstoreaccount1.blob.core.windows.net/attachments/plik-test.pdf");

            var blobClientMock = new Mock<BlobClient>(
                MockBehavior.Loose,
                blobUri,
                (BlobClientOptions)null!);

            blobClientMock.Setup(b => b.Uri).Returns(blobUri);

            blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        eTag: new ETag("\"etag\""),
                        lastModified: DateTimeOffset.UtcNow,
                        contentHash: default,
                        versionId: null,
                        encryptionKeySha256: default,
                        encryptionScope: null,
                        blobSequenceNumber: 0),
                    Mock.Of<Response>()));

            blobClientMock
                .Setup(b => b.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var containerUri = new Uri("https://devstoreaccount1.blob.core.windows.net/attachments");
            var containerMock = new Mock<BlobContainerClient>(
                MockBehavior.Loose,
                containerUri,
                (BlobClientOptions)null!);

            containerMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<BlobContainerInfo>(null!, Mock.Of<Response>()));

            containerMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            var serviceClientMock = new Mock<BlobServiceClient>(
                MockBehavior.Loose,
                "UseDevelopmentStorage=true");

            serviceClientMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerMock.Object);

            return new BlobStorageService(serviceClientMock.Object);
        }

        [Fact]
        public async Task UploadAsync_ValidFile_ReturnsUrl()
        {
            var service = CreateServiceWithMocks(out var expectedUri);
            var file = CreateFormFile("raport.pdf", Encoding.UTF8.GetBytes("pdf"));

            var url = await service.UploadAsync(file, "attachments");

            Assert.Equal(expectedUri.ToString(), url);
        }

        [Fact]
        public async Task DeleteAsync_ValidUrl_ReturnsTrue()
        {
            var service = CreateServiceWithMocks(out var blobUri);
            var file = CreateFormFile("do-usuniecia.pdf", Encoding.UTF8.GetBytes("x"));
            await service.UploadAsync(file, "attachments");

            var deleted = await service.DeleteAsync(blobUri.ToString(), "attachments");

            Assert.True(deleted);
        }
    }
}