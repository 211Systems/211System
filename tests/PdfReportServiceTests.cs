using System;
using System.IO;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models;
using _211system.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace _211system.Tests
{
    public class PdfReportServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new _211DbContext(options);

            if (!context.SeverityLevels.Any())
            {
                context.SeverityLevels.AddRange(
                    new SeverityLevel { Id = 1, Name = "Niski", ColorCode = "info" },
                    new SeverityLevel { Id = 2, Name = "Średni", ColorCode = "warning" },
                    new SeverityLevel { Id = 3, Name = "Wysoki", ColorCode = "danger" },
                    new SeverityLevel { Id = 4, Name = "Krytyczny", ColorCode = "dark" }
                );
                context.IncidentTypes.AddRange(
                    new IncidentType { Id = 1, Name = "Wypadek" },
                    new IncidentType { Id = 2, Name = "Pożar" },
                    new IncidentType { Id = 3, Name = "Zalanie" }
                );
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task GenerateIncidentsReportAsync_ShouldGeneratePdfAndSaveToDatabase()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var context = GetInMemoryDbContext();
            var service = new PdfReportService(context);

            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);

            context.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "ZGL/2024/001",
                Description = "Auto wpadło do rowu",
                SeverityLevelId = 3, // Zamiast Severity = "Wysoki"
                IncidentTypeId = 1,  // Wypadek
                ReportDate = new DateTime(2024, 6, 15),
                Status = "Zakończone"
            });

            context.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "ZGL/2024/002",
                Description = "Płonie poddasze",
                SeverityLevelId = 4, // Krytyczny
                IncidentTypeId = 2,  // Pożar
                ReportDate = new DateTime(2024, 7, 10),
                Status = "Nowe"
            });

            context.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "ZGL/2023/001",
                Description = "Woda w piwnicy",
                SeverityLevelId = 2, // Średni
                IncidentTypeId = 3,  // Zalanie
                ReportDate = new DateTime(2023, 5, 5),
                Status = "Zakończone"
            });

            await context.SaveChangesAsync();

            var result = await service.GenerateIncidentsReportAsync(fromDate, toDate);

            Assert.NotNull(result.FileBytes);
            Assert.True(result.FileBytes.Length > 0);
            Assert.Contains(".pdf", result.FileName);

            var savedReport = await context.PeriodicReports.FirstOrDefaultAsync();
            Assert.NotNull(savedReport);
            Assert.Equal(result.FileName, Path.GetFileName(savedReport.PathToPDF));

            if (File.Exists(savedReport.PathToPDF))
            {
                File.Delete(savedReport.PathToPDF);
            }
        }

        [Fact]
        public async Task GenerateIncidentsReportAsync_WhenNoIncidentsMatch_ShouldStillGenerateEmptyReport()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var context = GetInMemoryDbContext();
            var service = new PdfReportService(context);

            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);

            var result = await service.GenerateIncidentsReportAsync(fromDate, toDate);

            Assert.NotNull(result.FileBytes);
            Assert.True(result.FileBytes.Length > 0);

            var savedReport = await context.PeriodicReports.FirstOrDefaultAsync();
            Assert.NotNull(savedReport);

            if (File.Exists(savedReport!.PathToPDF))
            {
                File.Delete(savedReport.PathToPDF);
            }
        }
    }
}