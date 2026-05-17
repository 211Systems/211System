using _211system.Models.Aviation;
using _211system.Models.Dtos.Aviation;

namespace _211system.Models.Interfaces
{
    public interface IAviationService
    {
        Task<Airbase> CreateAirbaseAsync(CreateAirbaseDto dto);
        Task<IEnumerable<Airbase>> GetAllAirbasesAsync();
        Task<AirUnit> CreateAirUnitAsync(CreateAirUnitDto dto);
        Task<IEnumerable<AirUnitDto>> GetAllAirUnitsAsync();
        Task AssignAirUnitToIncidentAsync(Guid unitId, Guid incidentId);
        Task ReturnToBaseAsync(Guid unitId);
    }
}