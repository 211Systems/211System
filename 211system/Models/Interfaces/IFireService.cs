using _211system.Models.Dtos.Fire;
using FireDepartment;

namespace _211system.Models.Interfaces
{
    public interface IFireService
    {
        Task<FDepartment> CreateDepartmentAsync(CreateFDepartmentDto dto);
        Task<IEnumerable<FDepartmentDto>> GetAllDepartmentsAsync();
        Task<FiremanCreatedDto> CreateFiremanAsync(CreateFiremanDto dto);
        Task<IEnumerable<FiremanDto>> GetAllFiremenAsync();
        Task DeleteFiremanAsync(Guid id);
        Task<FireTruck> CreateFireTruckAsync(CreateFireTruckDto dto);
        Task<IEnumerable<FireTruckDto>> GetAllFireTrucksAsync();

        Task UpdateFireTruckAsync(Guid id, UpdateFireTruckDto dto);
        Task DeleteFireTruckAsync(Guid id);
        Task AssignFireTruckToIncidentAsync(Guid truckId, Guid incidentId);
        Task ReturnToBaseAsync(Guid operationId);
    }
}