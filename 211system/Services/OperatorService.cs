using _211system.Data;
using _211system.DTOs;
using _211system.Models.Interfaces;
using CPR112.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _211system.Models;

namespace _211system.Services;

public interface IOperatorService
{
    Task<IEnumerable<OperatorDto>> GetAllAsync();
    Task<(OperatorDto Operator, string TempPassword)> CreateAsync(CreateOperatorDto dto);
    Task<bool> DeleteAsync(Guid id);
    
    Task<bool> ChangeRankAsync(Guid id, string newRank);
}

public class OperatorService : IOperatorService
{
    private readonly _211DbContext _context;
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OperatorService(_211DbContext context, IAuthService authService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _authService = authService;
        _userManager = userManager;
    }

  public async Task<IEnumerable<OperatorDto>> GetAllAsync()
    {
        var ops = await _context.Operators112
            .Include(o => o.OpAccount) 
            .ToListAsync();

        return ops.Select(o => new OperatorDto
        {
            Id = o.Id,
            FirstName = o.FirstName,
            LastName = o.LastName,
            StationNumber = o.StationNumber,
            OpAccountId = o.OpAccountId,
            EncId = o.EncId,
            Rank = o.Rank.ToString(),
            Email = o.OpAccount?.Email ?? "Brak"
        });
    }

    public async Task<(OperatorDto Operator, string TempPassword)> CreateAsync(CreateOperatorDto dto)
    {
        var encExists = await _context.Encs.AnyAsync(e => e.Id == dto.EncId);
        if (!encExists)
            throw new Exception("Podana placówka CPR nie istnieje!");

        var (accountId, tempPassword) = await _authService.CreateTemporaryAccountAsync(dto.Email, dto.Rank);

        if (!Enum.TryParse<OperatorRank>(dto.Rank, true, out var parsedRank))
        {
            parsedRank = OperatorRank.Dyspozytor112; 
        }

        var newOperator = new Operator112
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            StationNumber = dto.StationNumber,
            OpAccountId = accountId,
            EncId = dto.EncId,
            Rank = parsedRank 
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
            EncId = newOperator.EncId,
            Rank = newOperator.Rank.ToString()
        };

        return (operatorDto, tempPassword);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var operatorToDelete = await _context.Operators112.FindAsync(id);
        if (operatorToDelete == null)
            return false;

        var applicationUser = await _userManager.FindByIdAsync(operatorToDelete.OpAccountId);
        
        _context.Operators112.Remove(operatorToDelete);
        await _context.SaveChangesAsync();

        if (applicationUser != null)
        {
            await _userManager.DeleteAsync(applicationUser);
        }

        return true;
    }

    public async Task<bool> ChangeRankAsync(Guid id, string newRank)
    {
        var operatorToUpdate = await _context.Operators112.FindAsync(id);
        if (operatorToUpdate == null) 
            return false;

        if (Enum.TryParse<OperatorRank>(newRank, true, out var parsedRank))
        {
            operatorToUpdate.Rank = parsedRank;
        }

        var applicationUser= await _userManager.FindByIdAsync(operatorToUpdate.OpAccountId);
        if (applicationUser != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(applicationUser);
            await _userManager.RemoveFromRolesAsync(applicationUser, currentRoles);
            await _userManager.AddToRoleAsync(applicationUser, newRank);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}