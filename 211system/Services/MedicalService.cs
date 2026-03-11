using _211system.Data;
using _211system.DTOs.Hospital;
using _211system.Models.Hospital;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public class MedicalService : IMedicalService
    {
        private readonly _211DbContext _context;

        public MedicalService(_211DbContext context)
        {
            _context = context;
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
        /*
        public async Task<ParamedicDto> CreateParamedicAsync(CreateParamedicDto dto)
        {
            var paramedic = new Paramedic
            {
                Name = dto.Name,
                LastName = dto.LastName,
                LicenseNumber = dto.LicenseNumber,
                Specialization = dto.Specialization,
                ParaAccountId = dto.ParaAccountId,
                HospitalId = dto.HospitalId
            };

            await _context.Paramedics.AddAsync(paramedic);
            await _context.SaveChangesAsync();

            return new ParamedicDto { Id = paramedic.Id, Name = paramedic.Name, LastName = paramedic.LastName, LicenseNumber = paramedic.LicenseNumber, Specialization = paramedic.Specialization, ParaAccountId = paramedic.ParaAccountId, HospitalId = paramedic.HospitalId };
        }
        */

        //testeowe później do usunięcia
        public async Task<ParamedicDto> CreateParamedicAsync(CreateParamedicDto dto)
        {
            var accountExists = await _context.Users.AnyAsync(u => u.Id == dto.ParaAccountId);

            if (!accountExists)
            {
                var dummyAccount = new Microsoft.AspNetCore.Identity.IdentityUser
                {
                    Id = dto.ParaAccountId, // Używamy Twojego stringa z payloadu Swaggera
                    UserName = "du" + dto.ParaAccountId + "@test.com",
                    Email = "du" + dto.ParaAccountId + "@test.com",
                    NormalizedUserName = "DU" + dto.ParaAccountId.ToUpper() + "@TEST.COM",
                    NormalizedEmail = "DU" + dto.ParaAccountId.ToUpper() + "@TEST.COM"
                };
                await _context.Users.AddAsync(dummyAccount);
                await _context.SaveChangesAsync(); // Zapisujemy atrapę konta jako pierwsi
            }
            var paramedic = new Paramedic
            {
                Name = dto.Name,
                LastName = dto.LastName,
                LicenseNumber = dto.LicenseNumber,
                Specialization = dto.Specialization,
                ParaAccountId = dto.ParaAccountId, // To ID teraz istnieje w AspNetUsers!
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
                ParaAccountId = paramedic.ParaAccountId,
                HospitalId = paramedic.HospitalId
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
    }
}