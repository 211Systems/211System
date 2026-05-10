using _211system.DTOs.Hospital;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _211system.Services
{
    public interface IMedicalService
    {
        Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto dto);
        Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync();
        Task<ParamedicDto> CreateParamedicAsync(CreateParamedicDto dto);
        Task<IEnumerable<ParamedicDto>> GetAllParamedicsAsync();
        Task<AmbulanceDto> CreateAmbulanceAsync(CreateAmbulanceDto dto);
        Task<IEnumerable<AmbulanceDto>> GetAllAmbulancesAsync();
        Task<Guid> StartMedicalOperationAsync(Guid paramedicId, Guid reportId);
        Task EndMedicalOperationAsync(Guid operationId);

        Task<IEnumerable<AmbulanceDto>> GetAvailableAmbulancesAsync();
        Task AssignAmbulanceToIncidentAsync(Guid ambulanceId, Guid incidentId);
        Task UpdateHospitalAsync(Guid id, UpdateHospitalDto dto);
        Task DeleteHospitalAsync(Guid id);

        Task DeleteParamedicAsync(Guid id);

        Task UpdateAmbulanceAsync(Guid id, UpdateAmbulanceDto dto);
        Task DeleteAmbulanceAsync(Guid id);

        Task<AmbulanceEquipmentDto> AddEquipmentAsync(Guid ambulanceId, CreateAmbulanceEquipmentDto dto);
        Task<IEnumerable<AmbulanceEquipmentDto>> GetEquipmentAsync(Guid ambulanceId);
        Task DeleteEquipmentAsync(Guid id);

        Task UpdateParamedicAsync(Guid id, UpdateParamedicDto dto);

        Task<IEnumerable<MedicalOperationDto>> GetAllOperationsAsync();
        Task TransportToHospitalAsync(Guid operationId, Guid targetHospitalId);
        Task ReturnToBaseAsync(Guid operationId);
    }
}