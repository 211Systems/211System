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
        Task FreeUnitAsync(Guid unitId);
        Task DeleteAirUnitAsync(Guid unitId);
        Task<AirUnit> UpdateAirUnitAsync(Guid unitId, UpdateAirUnitDto dto);
        Task UpdateUnitLocationAsync(Guid unitId, double lat, double lng, int? statusId);

        Task<IEnumerable<dynamic>> GetActiveOperationsAsync();
        Task TransportPatientAsync(Guid operationId, Guid hospitalId);
        Task ReturnToBaseAsync(Guid operationId);
        Task EndOperationAsync(Guid operationId);
    }
}