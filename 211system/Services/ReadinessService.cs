using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.DTOs;
using Police;

namespace _211system.Services;

public interface IReadinessService
{
    Task<List<ReadinessBoardDto>> GetReadinessBoardAsync();
}

public class ReadinessService : IReadinessService
{
    private readonly _211DbContext _context;

    public ReadinessService(_211DbContext context)
    {
        _context = context;
    }

    public async Task<List<ReadinessBoardDto>> GetReadinessBoardAsync()
    {

        var policeUnits = await _context.PoliceDepartments
            .Select(p => new ReadinessBoardDto
            {
                DepartmentId = p.PDepartmentId,
                Name = p.Name,
                Type = "Policja",
                
               
                Status = _context.PoliceOperations.Any(o => o.PDepartmentId == p.PDepartmentId && o.EndTime == null) 
                    ? "W akcji" 
                    : "Dostępny",
                
                CurrentIncidentId = _context.PoliceOperations
                    .Where(o => o.PDepartmentId == p.PDepartmentId && o.EndTime == null)
                    .Select(o => (Guid?)o.IncidentId)
                    .FirstOrDefault()
            }).ToListAsync();

        var fireUnits = await _context.FireDepartments
            .Select(f => new ReadinessBoardDto
            {
                DepartmentId = f.FDepartmentId,
                Name = f.Name,
                Type = "Straż Pożarna",
                Status = _context.FireOperations.Any(o => o.FDepartmentId == f.FDepartmentId && o.EndTime == null) 
                    ? "W akcji" 
                    : "Dostępny",
                CurrentIncidentId = _context.FireOperations
                    .Where(o => o.FDepartmentId == f.FDepartmentId && o.EndTime == null)
                    .Select(o => (Guid?)o.IncidentId)
                    .FirstOrDefault()
            }).ToListAsync();

        return policeUnits
            .Concat(fireUnits)
            .OrderBy(u => u.Status)
            .ThenBy(u => u.Type)
            .ThenBy(u => u.Name)
            .ToList();
    }
}