using Microsoft.AspNetCore.Mvc;
using _211system.DTOs;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperatorsController : Controller
{
    private readonly IOperatorService _operatorService;

    public OperatorsController(IOperatorService operatorService)
    {
        _operatorService = operatorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _operatorService.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorDto dto)
    {
        try
        {
            var result = await _operatorService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}