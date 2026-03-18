using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _211system.Data;
using _211system.Services;
using CPR112.Models;

namespace _211system.Tests
{
    public class PdfReportServiceTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
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
                Description = "Wypadek",
                Severity = "Wysoki",
                ReportDate = new DateTime(2024, 6, 15),
                Status = "Zakończone"
            });

            context.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "ZGL/2024/002",
                Description = "Pożar",
                Severity = "Krytyczny",
                ReportDate = new DateTime(2024, 7, 10),
                Status = "Nowe"
            });

            context.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "ZGL/2023/001",
                Description = "Zalanie",
                Severity = "Średni",
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