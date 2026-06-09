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
using _211system.Models.Aviation;

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

        var aviationOps = await _context.AviationOperations
         .Include(ao => ao.AirUnit)
         .Where(ao => ao.IncidentId.HasValue && incidentIds.Contains(ao.IncidentId.Value))
         .ToListAsync();

        var policeCars = await _context.PoliceCars.ToListAsync();
        var fireTrucks = await _context.FireTrucks.ToListAsync();
        var ambulances = await _context.Ambulances.ToListAsync();
        var crews = await _context.VehicleCrews.ToListAsync();
        var transports = await _context.TransportRecords
            .Where(t => incidentIds.Contains(t.IncidentId))
            .OrderBy(t => t.TransportedAt)
            .ToListAsync();

        string CrewSuffix(string vehicleType, Guid? vehicleId)
        {
            if (vehicleId == null) return "";
            var members = crews.Where(c => c.VehicleType == vehicleType && c.VehicleId == vehicleId.Value)
                               .Select(c => c.MemberName).ToList();
            return members.Any() ? " + obsada: " + string.Join(", ", members) : "";
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x));
                page.Footer().AlignCenter().Text(x => { x.Span("Strona "); x.CurrentPageNumber(); x.Span(" z "); x.TotalPages(); });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        string fileName = $"Raport_{from:yyyyMMdd}_{to:yyyyMMdd}_{Guid.NewGuid().ToString().Substring(0, 4)}.pdf";

        var reportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Raporty");
        if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);

        var filePath = Path.Combine(reportsFolder, fileName);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        var periodicReport = new PeriodicReport { Id = Guid.NewGuid(), PathToPDF = filePath, GenerationDate = DateTime.UtcNow };
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
                    column.Item().Text("Szczegółowy Raport Zdarzeń").FontSize(14);
                    column.Item().Text($"Za okres: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        static string FormatWeather(Incident incident)
        {
            var hasTemp = incident.WeatherTemperature.HasValue;
            var hasCond = !string.IsNullOrWhiteSpace(incident.WeatherCondition);
            if (!hasTemp && !hasCond) return "Brak danych";

            var lines = new List<string>();
            if (hasTemp)
                lines.Add($"{Math.Round(incident.WeatherTemperature!.Value, 1):0.#}°C");

            if (hasCond)
            {
                foreach (var part in incident.WeatherCondition!.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    lines.Add(part.Trim());
            }

            return string.Join("\n", lines);
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(48);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(4.2f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.0f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Numer");
                    header.Cell().Element(HeaderCell).Text("Data");
                    header.Cell().Element(HeaderCell).Text("Typ Zdarzenia");
                    header.Cell().Element(HeaderCell).Text("Pogoda");
                    header.Cell().Element(HeaderCell).Text("Służby (Zastępy)");
                    header.Cell().Element(HeaderCell).Text("Lokalizacja");
                    header.Cell().Element(HeaderCell).Text("Status");

                    static IContainer HeaderCell(IContainer c) => c
                        .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
                        .Background(Colors.Blue.Darken2)
                        .PaddingVertical(6)
                        .PaddingHorizontal(4);
                });

                var rowIndex = 0;
                foreach (var incident in incidents)
                {
                    var rowBg = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    rowIndex++;

                    var pOps = policeOps.Where(po => po.IncidentId == incident.Id).ToList();
                    var fOps = fireOps.Where(fo => fo.IncidentId == incident.Id).ToList();
                    var mOps = medicalOps.Where(mo => mo.ReportId == incident.Id).ToList();
                    var aOps = aviationOps.Where(ao => ao.IncidentId.HasValue && ao.IncidentId.Value == incident.Id).ToList();




                    string polStr = pOps.Any() ? string.Join("\n", pOps.Select(po => $"POL: {po.Policeman.Name} {po.Policeman.Lastname} ({policeCars.FirstOrDefault(c => c.PolicemanId == po.Policeman.Id)?.LicensePlate ?? "Brak"})")) : "POL: Brak";
                    string fireStr = fOps.Any() ? string.Join("\n", fOps.Select(fo => $"PSP: {fo.Fireman.Name} {fo.Fireman.Lastname} ({fireTrucks.FirstOrDefault(t => t.FiremanId == fo.Fireman.Id)?.LicensePlate ?? "Brak"})")) : "PSP: Brak";
                    string medStr = mOps.Any() ? string.Join("\n", mOps.Select(mo => $"ZRM: {mo.Paramedic.Name} {mo.Paramedic.LastName} ({ambulances.FirstOrDefault(a => a.ParamedicId == mo.Paramedic.Id)?.LicensePlate ?? "Brak"})")) : "ZRM: Brak";
                    string airStr = aOps.Any() ? string.Join("\n", aOps.Select(ao => $"{ao.AirUnit?.Callsign ?? "Brak"} ({ao.AirUnit?.ServiceType ?? 0})")) : "Brak";
                    string locText = $"GPS: {incident.Latitude}, {incident.Longitude}";

                    var servicesList = new List<string>();

                    if (pOps.Any())
                    {
                        servicesList.Add("POL: \n" + string.Join(", ", pOps.Select(po => {
                            var car = policeCars.FirstOrDefault(c => c.PolicemanId == po.Policeman.Id);
                            return $"{po.Policeman.Name} {po.Policeman.Lastname} (Radiowóz: {car?.LicensePlate ?? "Brak"}){CrewSuffix("police", car?.Id)}\n";
                        })));
                    }


                    if (fOps.Any())
                    {
                        servicesList.Add("PSP: \n" + string.Join(", ", fOps.Select(fo => {
                            var truck = fireTrucks.FirstOrDefault(t => t.FiremanId == fo.Fireman.Id);
                            return $"{fo.Fireman.Name} {fo.Fireman.Lastname} (Wóz Strażacki: {truck?.LicensePlate ?? "Brak"}){CrewSuffix("fire", truck?.Id)}\n";
                        })));
                    }

                    if (mOps.Any())
                    {
                        servicesList.Add("ZRM: \n" + string.Join(", ", mOps.Select(mo => {
                            var amb = ambulances.FirstOrDefault(a => a.ParamedicId == mo.Paramedic.Id);
                            return $"{mo.Paramedic.Name} {mo.Paramedic.LastName} (Ambulans: {amb?.LicensePlate ?? "Brak"}){CrewSuffix("ambulance", amb?.Id)}\n";
                        })));
                    }

                    if (aOps.Any())
                    {
                        servicesList.Add("LOT: \n" + string.Join(", ", aOps.Select(ao => {
                            var pilot = string.IsNullOrEmpty(ao.AirUnit?.PilotName) ? "brak pilota" : ao.AirUnit.PilotName;
                            return $"{ao.AirUnit?.Callsign ?? "Brak"} [{ao.AirUnit?.ServiceType}] (pilot: {pilot}){CrewSuffix("air", ao.AirUnit?.Id)}\n";
                        })));
                    }

                    string servicesText = servicesList.Any() ? string.Join("\n", servicesList) : "Brak zadysponowanych służb";

                    var transportRecs = transports.Where(t => t.IncidentId == incident.Id).ToList();
                    if (transportRecs.Any())
                    {
                        servicesText += "\n\nTransport:\n" + string.Join("\n", transportRecs.Select(t =>
                            $"→ {t.DestinationName} ({t.VehicleLabel}, {t.TransportedAt:dd.MM HH:mm})"));
                    }

                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(incident.IncidentNumber ?? "—");
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(incident.ReportDate.ToString("dd.MM\nHH:mm"));
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text($"{incident.IncidentType?.Name ?? "Brak"}\n(P: {incident.SeverityLevel?.Name ?? "—"})");
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(FormatWeather(incident));
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(servicesText);
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(
                        incident.Latitude != 0 || incident.Longitude != 0
                            ? $"GPS:\n{incident.Latitude:0.#####},\n{incident.Longitude:0.#####}"
                            : "Brak");
                    table.Cell().Element(c => BodyCell(c, rowBg)).Text(incident.Status ?? "—");
                }

                static IContainer BodyCell(IContainer container, string background) =>
                    container.Background(background)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(5)
                        .PaddingHorizontal(4);
            });
        }
    }
}