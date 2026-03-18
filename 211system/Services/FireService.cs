using _211system.Data;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using _211system.Services;
using FireDepartment;
using Microsoft.EntityFrameworkCore;

namespace _211system.Models.Services
{
    public class FireService : IFireService
    {
        private readonly _211DbContext _context;
        private readonly IAuthService _authService;

        public FireService(_211DbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<FDepartment> CreateDepartmentAsync(CreateFDepartmentDto dto)
        {
            var department = new FDepartment
            {
                Name = dto.Name,
                Address = dto.Address,
                District = dto.District
            };
            await _context.FireDepartments.AddAsync(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<FiremanCreatedDto> CreateFiremanAsync(CreateFiremanDto dto)
        {
            var department = await _context.FireDepartments.FindAsync(dto.FDepartmentId);
            if (department == null) throw new Exception("Remiza o podanym ID nie istnieje!");

            var accountResult = await _authService.CreateTemporaryAccountAsync(dto.Email, dto.Rank);

            var fireman = new Fireman
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Surname = dto.Surname,
                BadgeNumber = dto.BadgeNumber,
                Rank = dto.Rank,
                FDepartmentId = dto.FDepartmentId,
                FireAccountId = accountResult.AccountId,
                Department = department
            };

            await _context.Firemen.AddAsync(fireman);
            await _context.SaveChangesAsync();

            return new FiremanCreatedDto
            {
                Id = fireman.Id,
                Email = dto.Email,
                TemporaryPassword = accountResult.TemporaryPassword
            };
        }

        public async Task DeleteFiremanAsync(Guid id)
        {
            var fireman = await _context.Firemen.FindAsync(id);
            if (fireman != null)
            {
                _context.Firemen.Remove(fireman);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<FireTruck> CreateFireTruckAsync(CreateFireTruckDto dto)
        {
            var department = await _context.FireDepartments.FindAsync(dto.FDepartmentId);

            if (department == null)
                throw new Exception("Remiza o podanym ID nie istnieje!");

            var fireTruck = new FireTruck
            {
                LicensePlate = dto.LicensePlate,
                FDepartmentId = dto.FDepartmentId,
                Department = department
            };

            await _context.FireTrucks.AddAsync(fireTruck);
            await _context.SaveChangesAsync();
            return fireTruck;
        }

        public async Task<IEnumerable<FDepartmentDto>> GetAllDepartmentsAsync()
        {
            return await _context.FireDepartments
                .Select(d => new FDepartmentDto
                {
                    Id = d.FDepartmentId,
                    Name = d.Name,
                    Address = d.Address,
                    District = d.District
                }).ToListAsync();
        }

        public async Task<IEnumerable<FiremanDto>> GetAllFiremenAsync()
        {
            return await _context.Firemen
                .Select(f => new FiremanDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Surname = f.Surname,
                    BadgeNumber = f.BadgeNumber,
                    Rank = f.Rank,
                    FDepartmentId = f.FDepartmentId,
                    FireAccountId = f.FireAccountId
                }).ToListAsync();
        }

        public async Task<IEnumerable<FireTruckDto>> GetAllFireTrucksAsync()
        {
            return await _context.FireTrucks
                .Select(t => new FireTruckDto
                {
                    Id = t.Id,
                    LicensePlate = t.LicensePlate,
                    FDepartmentId = t.FDepartmentId
                }).ToListAsync();
        }
    }
}