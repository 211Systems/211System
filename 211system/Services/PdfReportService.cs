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

        var incidentIds = incidents.Select(i => i.Id).ToList();

        var policeOps = await _context.PoliceOperations.Include(po => po.Policeman).Where(po => incidentIds.Contains(po.IncidentId)).ToListAsync();
        var fireOps = await _context.FireOperations.Include(fo => fo.Fireman).Where(fo => incidentIds.Contains(fo.IncidentId)).ToListAsync();
        var medicalOps = await _context.MedicalOperations.Include(mo => mo.Paramedic).Where(mo => incidentIds.Contains(mo.ReportId)).ToListAsync();

        var policeCars = await _context.PoliceCars.ToListAsync();
        var fireTrucks = await _context.FireTrucks.ToListAsync();
        var ambulances = await _context.Ambulances.ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x));
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
                    column.Item().Text("Szczegółowy Raport Zdarzeń (Wszystkie Statusy)").FontSize(14);
                    column.Item().Text($"Za okres: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(70);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Numer");
                    header.Cell().Element(CellStyle).Text("Data");
                    header.Cell().Element(CellStyle).Text("Typ Zdarzenia");
                    header.Cell().Element(CellStyle).Text("Służby na Miejscu (Załoga + Pojazd)");
                    header.Cell().Element(CellStyle).Text("Lokalizacja i Pogoda");
                    header.Cell().Element(CellStyle).Text("Opis Zgłoszenia");

                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var incident in incidents)
                {
                    var pOp = policeOps.FirstOrDefault(po => po.IncidentId == incident.Id);
                    var fOp = fireOps.FirstOrDefault(fo => fo.IncidentId == incident.Id);
                    var mOp = medicalOps.FirstOrDefault(mo => mo.ReportId == incident.Id);

                    string polStr = "Brak";
                    if (pOp?.Policeman != null) {
                        var car = policeCars.FirstOrDefault(c => c.PolicemanId == pOp.Policeman.Id);
                        polStr = $"{pOp.Policeman.Name} {pOp.Policeman.Lastname} ({car?.LicensePlate ?? "Brak pojazdu"})";
                    }

                    string fireStr = "Brak";
                    if (fOp?.Fireman != null) {
                        var truck = fireTrucks.FirstOrDefault(t => t.FiremanId == fOp.Fireman.Id);
                        fireStr = $"{fOp.Fireman.Name} {fOp.Fireman.Lastname} ({truck?.LicensePlate ?? "Brak pojazdu"})";
                    }

                    string medStr = "Brak";
                    if (mOp?.Paramedic != null) {
                        var amb = ambulances.FirstOrDefault(a => a.ParamedicId == mOp.Paramedic.Id);
                        medStr = $"{mOp.Paramedic.Name} {mOp.Paramedic.LastName} ({amb?.LicensePlate ?? "Brak pojazdu"})";
                    }
                    
                    string servicesText = $"POL: {polStr}\nPSP: {fireStr}\nZRM: {medStr}";

                    string weatherText = incident.WeatherTemperature.HasValue ? $"{incident.WeatherTemperature}°C, {incident.WeatherCondition}" : "Brak danych z radaru";
                    string locText = $"GPS: {incident.Latitude}, {incident.Longitude}\nPogoda: {weatherText}";

                    table.Cell().Element(CellStyle).Text(incident.IncidentNumber ?? "Brak");
                    table.Cell().Element(CellStyle).Text(incident.ReportDate.ToString("dd.MM.yyyy\nHH:mm"));
                    table.Cell().Element(CellStyle).Text($"{incident.IncidentType?.Name ?? "Brak"}\n(P: {incident.SeverityLevel?.Name ?? "-"})");
                    table.Cell().Element(CellStyle).Text(servicesText);
                    table.Cell().Element(CellStyle).Text(locText);
                    
                    var desc = incident.Description?.Length > 60 ? incident.Description.Substring(0, 60) + "..." : incident.Description;
                    table.Cell().Element(CellStyle).Text(desc ?? "");

                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }
    }
}