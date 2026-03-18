using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using _211system.DTOs;
using _211system.Services;
using System;
namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] 
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
        try
        {
            var (newOperator, tempPassword) = await _operatorService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetAll), 
                new { id = newOperator.Id }, 
                new 
                { 
                    operatorDetails = newOperator, 
                    temporaryPassword = tempPassword 
                }
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}