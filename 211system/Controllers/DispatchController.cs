using Microsoft.AspNetCore.Mvc;
using _211system.DTOs;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatchController : Controller
{
    private readonly IDispatchService _dispatchService;

    public DispatchController(IDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [HttpPost("police/start")]
    public async Task<IActionResult> StartPolice([FromBody] StartPoliceOperationDto dto)
    {
        try
        {
            var id = await _dispatchService.StartPoliceOperationAsync(dto);
            return Ok(new { OperationId = id, Message = "Policja w drodze!" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("police/{id}/end")]
    public async Task<IActionResult> EndPolice(Guid id)
    {
        try
        {
            await _dispatchService.EndPoliceOperationAsync(id);
            return Ok(new { Message = "Policja wróciła." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("fire/start")]
    public async Task<IActionResult> StartFire([FromBody] StartFireOperationDto dto)
    {
        try
        {
            var id = await _dispatchService.StartFireOperationAsync(dto);
            return Ok(new { OperationId = id, Message = "Straż w drodze!" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("fire/{id}/end")]
    public async Task<IActionResult> EndFire(Guid id)
    {
        try
        {
            await _dispatchService.EndFireOperationAsync(id);
            return Ok(new { Message = "Straż wróciła." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}