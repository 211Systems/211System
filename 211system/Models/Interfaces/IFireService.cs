using _211system.Models.Dtos.Fire;
using FireDepartment;

namespace _211system.Models.Interfaces
{
    public interface IFireService
    {
        Task<FDepartment> CreateDepartmentAsync(CreateFDepartmentDto dto);
        Task<Fireman> CreateFiremanAsync(CreateFiremanDto dto);
        Task<FireTruck> CreateFireTruckAsync(CreateFireTruckDto dto);

        Task<IEnumerable<FDepartmentDto>> GetAllDepartmentsAsync();
        Task<IEnumerable<FiremanDto>> GetAllFiremenAsync();
        Task<IEnumerable<FireTruckDto>> GetAllFireTrucksAsync();
    }
}