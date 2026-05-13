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
        var incidents = await _context.Incidents
            .Include(i => i.IncidentType)
            .Include(i => i.SeverityLevel)
            .Where(i => i.ReportDate >= from && i.ReportDate <= to)
            .OrderBy(i => i.ReportDate)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, incidents));
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
                    column.Item().Text("Centrum Powiadamiania Ratunkowego 112").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Szczegółowy Raport Zdarzeń (Wszystkie Statusy)").FontSize(14);
                    column.Item().Text($"Za okres: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        void ComposeContent(IContainer container, System.Collections.Generic.List<Incident> incidents)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(90);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Numer");
                    header.Cell().Element(CellStyle).Text("Data");
                    header.Cell().Element(CellStyle).Text("Typ");
                    header.Cell().Element(CellStyle).Text("Priorytet");
                    header.Cell().Element(CellStyle).Text("Służby");
                    header.Cell().Element(CellStyle).Text("Położenie");
                    header.Cell().Element(CellStyle).Text("Opis");

                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var incident in incidents)
                {
                    table.Cell().Element(CellStyle).Text(incident.IncidentNumber ?? "Brak");
                    table.Cell().Element(CellStyle).Text(incident.ReportDate.ToString("dd.MM.yyyy HH:mm"));
                    table.Cell().Element(CellStyle).Text(incident.IncidentType?.Name ?? "Brak");
                    table.Cell().Element(CellStyle).Text(incident.SeverityLevel?.Name ?? "Brak");
                    
                    string services = "";
                    if (incident.IsPoliceActive) services += "POL ";
                    if (incident.IsFireActive) services += "PSP ";
                    if (incident.IsMedicalActive) services += "ZRM ";
                    table.Cell().Element(CellStyle).Text(string.IsNullOrEmpty(services) ? "-" : services);
                    
                    var locationInfo = incident.Latitude != 0 && incident.Longitude != 0 ? $"GPS: {incident.Latitude}, {incident.Longitude}" : "Brak lokalizacji";
                    table.Cell().Element(CellStyle).Text(locationInfo); 
                    
                    var desc = incident.Description?.Length > 50 ? incident.Description.Substring(0, 50) + "..." : incident.Description;
                    table.Cell().Element(CellStyle).Text(desc ?? "");

                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }
    }
}