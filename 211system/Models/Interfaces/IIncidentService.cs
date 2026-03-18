using System;
using System.Threading.Tasks;
using _211system.DTOs.CPR112;

namespace _211system.Services
{
    public interface IIncidentService
    {
        Task<IncidentDto> CreateIncidentAsync(CreateIncidentDto dto);
        
        Task<IncidentDto> GetIncidentByIdAsync(Guid id);
        
        Task ChangeIncidentStatusAsync(Guid id, Guid operatorId, ChangeIncidentStatusDto dto);
    }
}