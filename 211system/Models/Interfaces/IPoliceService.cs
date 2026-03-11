using _211system.Models.Dtos.Police;
using Police;

namespace _211system.Models.Interfaces
{
    public interface IPoliceService
    {
        Task<PDepartment> CreateDepartmentAsync(CreatePDepartmentDto dto);
        Task<Policeman> CreatePolicemanAsync(CreatePolicemanDto dto);
        Task<PoliceCar> CreatePoliceCarAsync(CreatePoliceCarDto dto);

        Task<IEnumerable<PDepartmentDto>> GetAllDepartmentsAsync();
        Task<IEnumerable<PolicemanDto>> GetAllPolicemenAsync();
        Task<IEnumerable<PoliceCarDto>> GetAllPoliceCarsAsync();
    }
}
