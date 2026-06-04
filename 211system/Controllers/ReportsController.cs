using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Services;
using _211system.Data;
using Microsoft.AspNetCore.Authorization;
using _211system.Models.Aviation;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin, Admin112, Dyspozytor112, Naczelnik, Kapitan, Komendant, Inspektor, Kierownik Szpitala")]
public class ReportsController : Controller
{
    private readonly IPdfReportService _pdfReportService;
    private readonly _211DbContext _context;

    public ReportsController(IPdfReportService pdfReportService, _211DbContext context)
    {
        _pdfReportService = pdfReportService;
        _context = context;
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
            from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            to = DateTime.SpecifyKind(to.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            if (from > to)
                return BadRequest(new { message = "Data początkowa nie może być późniejsza niż końcowa." });

            var (fileBytes, fileName) = await _pdfReportService.GenerateIncidentsReportAsync(from, to);
            return File(fileBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetReportData([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
            from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            to = DateTime.SpecifyKind(to.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var incidents = await _context.Incidents
                .Include(i => i.IncidentType)
                .Include(i => i.SeverityLevel)
                .Where(i => i.ReportDate >= from && i.ReportDate <= to)
                .OrderByDescending(i => i.ReportDate)
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

            var result = incidents.Select(i => {
                var pOps = policeOps.Where(po => po.IncidentId == i.Id).ToList();
                var fOps = fireOps.Where(fo => fo.IncidentId == i.Id).ToList();
                var mOps = medicalOps.Where(mo => mo.ReportId == i.Id).ToList();
                var aOps = aviationOps.Where(ao => ao.IncidentId == i.Id).ToList();

                var servicesList = new List<string>();

                if (pOps.Any())
                    servicesList.Add("<b>POL:</b> " + string.Join(", ", pOps.Select(po => $"{po.Policeman.Name} {po.Policeman.Lastname} (Radiowóz: {policeCars.FirstOrDefault(c => c.PolicemanId == po.Policeman.Id)?.LicensePlate ?? "Brak"})")));

                if (fOps.Any())
                    servicesList.Add("<b>PSP:</b> " + string.Join(", ", fOps.Select(fo => $"{fo.Fireman.Name} {fo.Fireman.Lastname} (Wóz: {fireTrucks.FirstOrDefault(t => t.FiremanId == fo.Fireman.Id)?.LicensePlate ?? "Brak"})")));

                if (mOps.Any())
                    servicesList.Add("<b>ZRM:</b> " + string.Join(", ", mOps.Select(mo => $"{mo.Paramedic.Name} {mo.Paramedic.LastName} (Karetka: {ambulances.FirstOrDefault(a => a.ParamedicId == mo.Paramedic.Id)?.LicensePlate ?? "Brak"})")));

                if (aOps.Any())
                    servicesList.Add("<b>LOT:</b> " + string.Join(", ", aOps.Select(ao => $"{ao.AirUnit?.Callsign ?? "Brak"} ({ao.AirUnit?.ServiceType})")));

                string servicesText = servicesList.Any() ? string.Join("<br>", servicesList) : "Brak służb";

                return new
                {
                    incidentNumber = i.IncidentNumber,
                    date = i.ReportDate.ToString("yyyy-MM-dd HH:mm"),
                    type = i.IncidentType?.Name ?? "Brak",
                    severity = i.SeverityLevel?.Name ?? "-",
                    status = i.Status,
                    description = i.Description,
                    address = (i.Latitude != 0 && i.Longitude != 0) ? $"GPS: {i.Latitude}, {i.Longitude}" : "Brak",
                    weather = i.WeatherTemperature.HasValue ? $"{i.WeatherTemperature}°C, {i.WeatherCondition}" : "Brak danych",
                    services = servicesText
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}