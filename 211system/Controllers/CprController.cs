using Microsoft.AspNetCore.Mvc;
using _211system.DTOs;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        return Ok(await _encService.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEncDto dto)
    {
        var result = await _encService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }
}