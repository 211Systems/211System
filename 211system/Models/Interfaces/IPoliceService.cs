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
    }
}
