using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadinessController : Controller
{
    private readonly IReadinessService _readinessService;

    public ReadinessController(IReadinessService readinessService)
    {
        _readinessService = readinessService;
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard()
    {
        var board = await _readinessService.GetReadinessBoardAsync();
        return Ok(board);
    }
}