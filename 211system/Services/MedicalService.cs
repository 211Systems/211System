using _211system.Data;
using _211system.DTOs.Hospital;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public class MedicalService : IMedicalService
    {
        private readonly _211DbContext _context;
        private readonly IAuthService _authService;
        public MedicalService(_211DbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto dto)
        {
            var hospital = new Hospital
            {
                Name = dto.Name,
                HasSOR = dto.HasSOR,
                Address = dto.Address
            };

            await _context.Hospitals.AddAsync(hospital);
            await _context.SaveChangesAsync();
            return new HospitalDto { Id = hospital.Id, Name = hospital.Name, HasSOR = hospital.HasSOR, Address = hospital.Address };
        }
        public async Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync()
        {
            var hospitals = await _context.Hospitals.ToListAsync();

            return hospitals.Select(h => new HospitalDto
            {
                Id = h.Id,
                Name = h.Name,
                HasSOR = h.HasSOR,
                Address = h.Address
            });
        }

        public async Task<IEnumerable<ParamedicDto>> GetAllParamedicsAsync()
        {
            var paramedics = await _context.Paramedics
                .Include(p => p.ParaAccount)
                .ToListAsync();

            return paramedics.Select(p => new ParamedicDto
            {
                Id = p.Id,
                Name = p.Name,
                LastName = p.LastName,
                LicenseNumber = p.LicenseNumber,
                Specialization = p.Specialization,
                Rank = p.Rank,
                ParaAccountId = p.ParaAccountId,
                HospitalId = p.HospitalId,
                Email = p.ParaAccount?.Email ?? "Brak"
            });
        }
        public async Task<ParamedicDto> CreateParamedicAsync(CreateParamedicDto dto)
        {
            var accountResult = await _authService.CreateTemporaryAccountAsync(dto.Email, dto.Rank);

            var paramedic = new Paramedic
            {
                Name = dto.Name,
                LastName = dto.LastName,
                LicenseNumber = dto.LicenseNumber,
                Specialization = dto.Specialization,
                Rank = dto.Rank,
                ParaAccountId = accountResult.AccountId,
                HospitalId = dto.HospitalId
            };

            await _context.Paramedics.AddAsync(paramedic);
            await _context.SaveChangesAsync();

            return new ParamedicDto
            {
                Id = paramedic.Id,
                Name = paramedic.Name,
                LastName = paramedic.LastName,
                LicenseNumber = paramedic.LicenseNumber,
                Specialization = paramedic.Specialization,
                Rank = paramedic.Rank,
                HospitalId = paramedic.HospitalId,
                Email = dto.Email,
                ParaAccountId = accountResult.AccountId,
                TemporaryPassword = accountResult.TemporaryPassword
            };
        }

        public async Task<Guid> StartMedicalOperationAsync(Guid paramedicId, Guid reportId)
        {
            var paramedicExists = await _context.Paramedics.AnyAsync(p => p.Id == paramedicId);
            if (!paramedicExists) throw new ArgumentException("Ratownik nie istnieje.");

            var isBusy = await _context.MedicalOperations
                .AnyAsync(m => m.ParamedicId == paramedicId && m.EndTime == null);
            if (isBusy)
            {
                throw new InvalidOperationException("Ten ratownik jest już przypisany do innej, niezakończonej akcji!");
            }

            var operation = new MedicalOperation
            {
                ParamedicId = paramedicId,
                ReportId = reportId,
                StartTime = DateTime.UtcNow
            };
            await _context.MedicalOperations.AddAsync(operation);
            await _context.SaveChangesAsync();

            return operation.Id;
        }

        public async Task EndMedicalOperationAsync(Guid operationId)
        {
            var operation = await _context.MedicalOperations.FindAsync(operationId);
            if (operation == null) throw new ArgumentException("Nie znaleziono takiej operacji.");
            if (operation.EndTime != null)
            {
                throw new InvalidOperationException("Ta akcja została już wcześniej zakończona.");
            }
            operation.EndTime = DateTime.UtcNow;
            _context.MedicalOperations.Update(operation);
            await _context.SaveChangesAsync();
        }
        public async Task<AmbulanceDto> CreateAmbulanceAsync(CreateAmbulanceDto dto)
        {
            var ambulance = new Ambulance
            {
                Type = dto.Type,
                LicensePlate = dto.LicensePlate,
                HospitalId = dto.HospitalId
            };

            await _context.Ambulances.AddAsync(ambulance);
            await _context.SaveChangesAsync();

            return new AmbulanceDto
            {
                Id = ambulance.Id,
                Type = ambulance.Type,
                LicensePlate = ambulance.LicensePlate,
                HospitalId = ambulance.HospitalId
            };
        }

        public async Task<IEnumerable<AmbulanceDto>> GetAllAmbulancesAsync()
        {
            var ambulances = await _context.Ambulances.ToListAsync();
            return ambulances.Select(a => new AmbulanceDto
            {
                Id = a.Id,
                Type = a.Type,
                LicensePlate = a.LicensePlate,
                HospitalId = a.HospitalId
            });
        }
    }
}