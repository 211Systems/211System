using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : Controller
{
    private readonly IPdfReportService _pdfReportService;

    public ReportsController(IPdfReportService pdfReportService)
    {
        _pdfReportService = pdfReportService;
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
            from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

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
}