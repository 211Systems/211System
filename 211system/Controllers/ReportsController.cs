using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Services;
using _211system.Data;
using Microsoft.AspNetCore.Authorization;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
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

            var policeOps = await _context.PoliceOperations
                .Include(po => po.Policeman)
                .Where(po => incidentIds.Contains(po.IncidentId))
                .ToListAsync();

            var fireOps = await _context.FireOperations
                .Include(fo => fo.Fireman)
                .Where(fo => incidentIds.Contains(fo.IncidentId))
                .ToListAsync();

            var medicalOps = await _context.MedicalOperations
                .Include(mo => mo.Paramedic)
                .Where(mo => incidentIds.Contains(mo.ReportId)) 
                .ToListAsync();

            var policeCars = await _context.PoliceCars.ToListAsync();
            var fireTrucks = await _context.FireTrucks.ToListAsync();
            var ambulances = await _context.Ambulances.ToListAsync();

            var result = incidents.Select(i => {
                var pOp = policeOps.FirstOrDefault(po => po.IncidentId == i.Id);
                var fOp = fireOps.FirstOrDefault(fo => fo.IncidentId == i.Id);
                var mOp = medicalOps.FirstOrDefault(mo => mo.ReportId == i.Id);

                string policeStr = "Brak";
                if (pOp?.Policeman != null) 
                {
                    var car = policeCars.FirstOrDefault(c => c.PolicemanId == pOp.Policeman.Id);
                    string carInfo = car != null ? (car.LicensePlate ?? "Brak nr") : "Brak pojazdu"; 
                    policeStr = $"{pOp.Policeman.Name} {pOp.Policeman.Lastname} ({pOp.Policeman.Rank}) | Pojazd: {carInfo}";
                }
                else if (i.IsPoliceActive) 
                {
                    policeStr = "Zadysponowano";
                }
                string fireStr = "Brak";
                if (fOp?.Fireman != null) 
                {
                    var truck = fireTrucks.FirstOrDefault(t => t.FiremanId == fOp.Fireman.Id);
                    string truckInfo = truck != null ? (truck.LicensePlate ?? "Brak nr") : "Brak pojazdu";
                    fireStr = $"{fOp.Fireman.Name} {fOp.Fireman.Lastname} ({fOp.Fireman.Rank}) | Wóz: {truckInfo}";
                }
                else if (i.IsFireActive) 
                {
                    fireStr = "Zadysponowano";
                }

                string medicalStr = "Brak";
                if (mOp?.Paramedic != null) 
                {
                    var amb = ambulances.FirstOrDefault(a => a.ParamedicId == mOp.Paramedic.Id);
                    string ambInfo = amb != null ? (amb.LicensePlate ?? "Brak nr") : "Brak pojazdu";
                    medicalStr = $"{mOp.Paramedic.Name} {mOp.Paramedic.LastName} | Karetka: {ambInfo}"; 
                }
                else if (i.IsMedicalActive) 
                {
                    medicalStr = "Zadysponowano";
                }

                return new {
                    incidentNumber = i.IncidentNumber,
                    date = i.ReportDate.ToString("yyyy-MM-dd HH:mm"),
                    type = i.IncidentType != null ? i.IncidentType.Name : "Brak",
                    severity = i.SeverityLevel != null ? i.SeverityLevel.Name : "Brak",
                    status = i.Status,
                    description = i.Description,
                    address = (i.Latitude != 0 && i.Longitude != 0) ? $"GPS: {i.Latitude}, {i.Longitude}" : "Brak",
                    
                    weather = i.WeatherTemperature.HasValue 
                        ? $"{i.WeatherTemperature}°C, {i.WeatherCondition}" 
                        : "Brak danych z radaru",
                        
                    police = policeStr,
                    fire = fireStr,
                    medical = medicalStr
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