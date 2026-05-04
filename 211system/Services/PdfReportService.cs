using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _211system.Data;
using CPR112.Models;

namespace _211system.Services;

public interface IPdfReportService
{
    Task<(byte[] FileBytes, string FileName)> GenerateIncidentsReportAsync(DateTime from, DateTime to);
}

public class PdfReportService : IPdfReportService
{
    private readonly _211DbContext _context;

    public PdfReportService(_211DbContext context)
    {
        _context = context;
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateIncidentsReportAsync(DateTime from, DateTime to)
    {

        var closedIncidents = await _context.Incidents
            .Where(i => i.Status == "Zakończone" || i.Status == "Zakonczone")
            .Where(i => i.ReportDate >= from && i.ReportDate <= to)
            .OrderBy(i => i.ReportDate)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, closedIncidents));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                    x.Span(" z ");
                    x.TotalPages();
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        string fileName = $"Raport_{from:yyyyMMdd}_{to:yyyyMMdd}_{Guid.NewGuid().ToString().Substring(0,4)}.pdf";
        
        var reportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Raporty");
        if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);
        
        var filePath = Path.Combine(reportsFolder, fileName);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        var periodicReport = new PeriodicReport
        {
            Id = Guid.NewGuid(),
            PathToPDF = filePath,
            GenerationDate = DateTime.UtcNow
        };
        
        _context.PeriodicReports.Add(periodicReport);
        await _context.SaveChangesAsync();

        return (pdfBytes, fileName);

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Centrum Powiadamiania Ratunkowego").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Raport zamkniętych zgłoszeń").FontSize(14);
                    column.Item().Text($"Za okres: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}").FontSize(12);
                });
            });
        }

        void ComposeContent(IContainer container, System.Collections.Generic.List<Incident> incidents)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Nr Zgłoszenia");
                    header.Cell().Element(CellStyle).Text("Data");
                    header.Cell().Element(CellStyle).Text("Status");
                    header.Cell().Element(CellStyle).Text("Położenie");

                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var incident in incidents)
                {

                    table.Cell().Element(CellStyle).Text(incident.IncidentNumber ?? "Brak numeru");
                    table.Cell().Element(CellStyle).Text(incident.ReportDate.ToString("dd.MM.yyyy HH:mm"));
                    table.Cell().Element(CellStyle).Text(incident.Status ?? "Brak");
                    
                    var locationInfo = incident.Latitude != 0 && incident.Longitude != 0 ? $"GPS: {incident.Latitude}, {incident.Longitude}" : "Brak lokalizacji";
                    table.Cell().Element(CellStyle).Text(locationInfo); 

                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }
    }
}