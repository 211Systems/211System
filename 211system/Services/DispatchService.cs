using _211system.Data;
using _211system.DTOs;
using FireDepartment;
using Police;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services;

public interface IDispatchService
{
    Task<Guid> StartPoliceOperationAsync(StartPoliceOperationDto dto);
    Task EndPoliceOperationAsync(Guid operationId);
    
    Task<Guid> StartFireOperationAsync(StartFireOperationDto dto);
    Task EndFireOperationAsync(Guid operationId);
}

public class DispatchService : IDispatchService
{
    private readonly _211DbContext _context;

    public DispatchService(_211DbContext context)
    {
        _context = context;
    }

    public async Task<Guid> StartPoliceOperationAsync(StartPoliceOperationDto dto)
    {
        var isBusy = await _context.PoliceOperations
            .AnyAsync(o => o.PDepartmentId == dto.PDepartmentId && o.EndTime == null);

        if (isBusy)
            throw new Exception("Ta jednostka policji jest już w akcji!");

        var operation = new PoliceOperation
        {
            PDepartmentId = dto.PDepartmentId,
            IncidentId = dto.IncidentId,
            StartTime = DateTime.UtcNow,
            EndTime = null
        };

        _context.PoliceOperations.Add(operation);
        await _context.SaveChangesAsync();
        return operation.Id;
    }

    public async Task EndPoliceOperationAsync(Guid operationId)
    {
        var operation = await _context.PoliceOperations.FindAsync(operationId);
        if (operation == null) throw new Exception("Nie znaleziono operacji.");
        if (operation.EndTime != null) throw new Exception("Ta operacja już się zakończyła.");

        operation.EndTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<Guid> StartFireOperationAsync(StartFireOperationDto dto)
    {

        var isBusy = await _context.FireOperations
            .AnyAsync(o => o.FDepartmentId == dto.FDepartmentId && o.EndTime == null);

        if (isBusy)
            throw new Exception("Ta jednostka straży jest już w akcji (brak czasu powrotu)!");

        var operation = new FireDepartmentOperation
        {
            FDepartmentId = dto.FDepartmentId,
            IncidentId = dto.IncidentId,
            StartTime = DateTime.UtcNow,
            EndTime = null
        };

        _context.FireOperations.Add(operation);
        await _context.SaveChangesAsync();
        return operation.Id;
    }

    public async Task EndFireOperationAsync(Guid operationId)
    {
        var operation = await _context.FireOperations.FindAsync(operationId);
        if (operation == null) throw new Exception("Nie znaleziono operacji.");
        if (operation.EndTime != null) throw new Exception("Ta operacja już się zakończyła.");

        operation.EndTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}