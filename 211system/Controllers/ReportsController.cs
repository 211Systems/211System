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
                .Select(i => new
                {
                    incidentNumber = i.IncidentNumber,
                    date = i.ReportDate.ToString("yyyy-MM-dd HH:mm"),
                    type = i.IncidentType != null ? i.IncidentType.Name : "Brak",
                    severity = i.SeverityLevel != null ? i.SeverityLevel.Name : "Brak",
                    status = i.Status,
                    description = i.Description,
                    address = (i.Latitude != 0 && i.Longitude != 0) ? $"GPS: {i.Latitude}, {i.Longitude}" : "Brak",
                    police = i.IsPoliceActive ? "Tak" : "Nie",
                    fire = i.IsFireActive ? "Tak" : "Nie",
                    medical = i.IsMedicalActive ? "Tak" : "Nie"
                })
                .ToListAsync();

            return Ok(incidents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}