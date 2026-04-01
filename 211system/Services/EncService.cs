using _211system.Data;
using _211system.DTOs;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services;

public interface IEncService
{
    Task<IEnumerable<EncDto>> GetAllAsync();
    Task<EncDto> CreateAsync(CreateEncDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class EncService : IEncService
{
    private readonly _211DbContext _context;

    public EncService(_211DbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EncDto>> GetAllAsync()
    {
        var encs = await _context.Encs.ToListAsync();
        return encs.Select(e => new EncDto 
        { 
            Id = e.Id, 
            Name = e.Name, 
            Region = e.Region
        });
    }

    public async Task<EncDto> CreateAsync(CreateEncDto dto)
    {
        var newEnc = new Enc
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Region = dto.Region
        };

        _context.Encs.Add(newEnc);
        await _context.SaveChangesAsync();

        return new EncDto { Id = newEnc.Id, Name = newEnc.Name, Region = newEnc.Region };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var enc = await _context.Encs.FindAsync(id);
        if (enc == null) return false;

        _context.Encs.Remove(enc);
        await _context.SaveChangesAsync();
        return true;
    }
}