using _211system.DTOs.Hospital;

namespace _211system.Services
{
    public interface IMedicalService
    {
        Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto dto);
        Task<ParamedicDto> CreateParamedicAsync(CreateParamedicDto dto);
        Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync();
        Task<IEnumerable<ParamedicDto>> GetAllParamedicsAsync();
        Task<AmbulanceDto> CreateAmbulanceAsync(CreateAmbulanceDto dto);
        Task<IEnumerable<AmbulanceDto>> GetAllAmbulancesAsync();

        Task<Guid> StartMedicalOperationAsync(Guid paramedicId, Guid reportId);
        Task EndMedicalOperationAsync(Guid operationId);
    }
}