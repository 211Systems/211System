using Microsoft.AspNetCore.Mvc;
using _211system.DTOs;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;

namespace _211system.Controllers;

[Authorize(Roles = "Admin, Admin112")]
[ApiController]
[Route("api/Enc")]
public class CprController : Controller
{
    private readonly IEncService _encService;

    public CprController(IEncService encService)
    {
        _encService = encService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _encService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEncDto dto)
    {
        var result = await _encService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _encService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}