using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _211system.Models.Dtos.Police;
using Police;

namespace _211system.Models.Interfaces
{
    public interface IPoliceService
    {
        Task<PDepartment> CreateDepartmentAsync(CreatePDepartmentDto dto);
        Task<IEnumerable<PDepartmentDto>> GetAllDepartmentsAsync();

        Task<PolicemanCreatedDto> CreatePolicemanAsync(CreatePolicemanDto dto);
        Task<IEnumerable<PolicemanDto>> GetAllPolicemenAsync();
        Task DeletePolicemanAsync(Guid id);
        
        Task<PoliceCar> CreatePoliceCarAsync(CreatePoliceCarDto dto);
        Task<IEnumerable<PoliceCarDto>> GetAllPoliceCarsAsync();
        Task UpdatePoliceCarAsync(Guid id, UpdatePoliceCarDto dto);
        Task DeletePoliceCarAsync(Guid id);
        Task AssignPoliceCarToIncidentAsync(Guid carId, Guid incidentId);
        Task TransportToStationAsync(Guid operationId, Guid departmentId);
        Task ReturnToBaseAsync(Guid operationId);
    }
}