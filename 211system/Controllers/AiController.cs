using _211system.DTOs;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : Controller
{
    private readonly IOpenAiService _openAiService;

    public AiController(IOpenAiService openAiService)
    {
        _openAiService = openAiService;
    }

    [HttpPost("advise")]
    public async Task<IActionResult> GetAdvise([FromBody] AiAdviseRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            return BadRequest(new { Message = "Opis zdarzenia nie może być pusty." });
        }

        try
        {
            var result = await _openAiService.GetAdviceAsync(dto.Description);

            return Ok(new { Advice = result });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { Message = "Błąd bramy podczas łączenia z dostawcą AI.", Details = ex.Message });
        }
    }
}