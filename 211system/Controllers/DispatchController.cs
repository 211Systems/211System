using Microsoft.AspNetCore.Mvc;
using _211system.DTOs;
using _211system.Services;
using _211system.Data;
using Microsoft.EntityFrameworkCore;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatchController : ControllerBase
{
    private readonly IDispatchService _dispatchService;
    private readonly _211DbContext _context;

    public DispatchController(IDispatchService dispatchService, _211DbContext context)
    {
        _dispatchService = dispatchService;
        _context = context;
    }

    [HttpPost("police/start")]
    public async Task<IActionResult> StartPolice([FromBody] StartPoliceOperationDto dto)
    {
        try
        {
            var opId = await _dispatchService.StartPoliceOperationAsync(dto);

            var incident = await _context.Incidents.FindAsync(dto.IncidentId);
            if (incident != null)
            {
                incident.IsPoliceActive = true;
                incident.Status = "W toku";
                await _context.SaveChangesAsync();
            }

            return Ok(new { OperationId = opId, Message = "Jednostki Policji zostały zadysponowane." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("police/{id}/end")]
    public async Task<IActionResult> EndPolice(Guid id)
    {
        try
        {
            var operation = await _context.PoliceOperations.FindAsync(id);
            if (operation == null) return NotFound("Nie znaleziono operacji policyjnej.");
            
            var incidentId = operation.IncidentId;
            await _dispatchService.EndPoliceOperationAsync(id);

            await CheckAndCloseIncidentAsync(incidentId, "police");

            return Ok(new { Message = "Interwencja Policji zakończona." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("fire/start")]
    public async Task<IActionResult> StartFire([FromBody] StartFireOperationDto dto)
    {
        try
        {
            var opId = await _dispatchService.StartFireOperationAsync(dto);

            var incident = await _context.Incidents.FindAsync(dto.IncidentId);
            if (incident != null)
            {
                incident.IsFireActive = true;
                incident.Status = "W toku";
                await _context.SaveChangesAsync();
            }

            return Ok(new { OperationId = opId, Message = "Zastępy PSP/OSP w drodze." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("fire/{id}/end")]
    public async Task<IActionResult> EndFire(Guid id)
    {
        try
        {
            var operation = await _context.FireOperations.FindAsync(id);
            if (operation == null) return NotFound("Nie znaleziono operacji straży.");

            var incidentId = operation.IncidentId;

            await _dispatchService.EndFireOperationAsync(id);

            await CheckAndCloseIncidentAsync(incidentId, "fire");

            return Ok(new { Message = "Działania gaśnicze/ratownicze zakończone." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task CheckAndCloseIncidentAsync(Guid incidentId, string serviceType)
    {
        var incident = await _context.Incidents.FindAsync(incidentId);
        if (incident == null) return;

        if (serviceType == "police") incident.IsPoliceActive = false;
        if (serviceType == "fire") incident.IsFireActive = false;
        if (serviceType == "medical") incident.IsMedicalActive = false;

        if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
        {
            incident.Status = "Zakończone";
        }

        await _context.SaveChangesAsync();
    }
}