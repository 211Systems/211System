using _211system.Data;
using _211system.DTOs;
using _211system.Models.Interfaces;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services;

public interface IOperatorService
{
    Task<IEnumerable<OperatorDto>> GetAllAsync();
    Task<(OperatorDto Operator, string TempPassword)> CreateAsync(CreateOperatorDto dto);
}

public class OperatorService : IOperatorService
{
    private readonly _211DbContext _context;
    private readonly IAuthService _authService;

    public OperatorService(_211DbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<IEnumerable<OperatorDto>> GetAllAsync()
    {
        var ops = await _context.Operators112.ToListAsync();
        return ops.Select(o => new OperatorDto
        {
            Id = o.Id,
            FirstName = o.FirstName,
            LastName = o.LastName,
            StationNumber = o.StationNumber,
            OpAccountId = o.OpAccountId,
            EncId = o.EncId
        });
    }

    public async Task<(OperatorDto Operator, string TempPassword)> CreateAsync(CreateOperatorDto dto)
    {
        var encExists = await _context.Encs.AnyAsync(e => e.Id == dto.EncId);
        if (!encExists)
            throw new Exception("Podana placówka CPR nie istnieje!");

        var (accountId, tempPassword) = await _authService.CreateTemporaryAccountAsync(dto.Email, "Dyspozytor112");

        var newOperator = new Operator112
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            StationNumber = dto.StationNumber,
            OpAccountId = accountId,
            EncId = dto.EncId
        };

        _context.Operators112.Add(newOperator);
        await _context.SaveChangesAsync();

        var operatorDto = new OperatorDto
        {
            Id = newOperator.Id,
            FirstName = newOperator.FirstName,
            LastName = newOperator.LastName,
            StationNumber = newOperator.StationNumber,
            OpAccountId = newOperator.OpAccountId,
            EncId = newOperator.EncId
        };

        return (operatorDto, tempPassword);
    }
}