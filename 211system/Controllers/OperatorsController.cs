using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using _211system.DTOs;
using _211system.Models.Interfaces;
using System;
using System.Threading.Tasks;
using _211system.Services;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Admin112")] 
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
        var operators = await _operatorService.GetAllAsync();
        return Ok(operators);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var (newOperator, tempPassword) = await _operatorService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetAll),
                new { id = newOperator.Id },
                new
                {
                    message = "Operator utworzony pomyślnie!",
                    operatorDetails = newOperator,
                    temporaryPassword = tempPassword
                }
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "Nie udało się utworzyć operatora.", 
                error = ex.Message 
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _operatorService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Nie znaleziono takiego pracownika w bazie." });
            }

            return Ok(new { message = "Pracownik został pomyślnie usunięty." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania", error = ex.Message });
        }
    }

    [HttpPut("{id}/rank")]
    public async Task<IActionResult> ChangeRank(Guid id, [FromBody] ChangeRankDto dto)
    {
        try
        {
            var result = await _operatorService.ChangeRankAsync(id, dto.NewRank);
            if (!result) 
            {
                return NotFound(new { message = "Nie znaleziono pracownika." });
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Błąd podczas zmiany rangi.", error = ex.Message });
        }
    }
}

public class ChangeRankDto
{
    public string NewRank { get; set; }
}