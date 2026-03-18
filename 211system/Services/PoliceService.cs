using System;
using _211system.Data;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Police;

namespace _211system.Models.Services

{
    public class PoliceService : IPoliceService
    {
        private readonly _211DbContext _context;
        private readonly IAuthService _authService;

        public PoliceService(_211DbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }


        public async Task<PDepartment> CreateDepartmentAsync(CreatePDepartmentDto dto)
        {
            var department = new PDepartment
            {
                Name = dto.Name,
                Address = dto.Address,
                District = dto.District
            };
            await _context.PoliceDepartments.AddAsync(department);
            await _context.SaveChangesAsync();

            return department;
        }

        public async Task<PolicemanCreatedDto> CreatePolicemanAsync(CreatePolicemanDto dto)
        {
            var department = await _context.PoliceDepartments.FindAsync(dto.PDepartmentId);
            if (department == null) throw new Exception("Komenda o podanym ID nie istnieje!");

            var accountResult = await _authService.CreateTemporaryAccountAsync(dto.Email, dto.Rank);

            var policeman = new Policeman
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Surname = dto.Surname,
                BadgeNumber = dto.BadgeNumber,
                Rank = dto.Rank,
                PDepartmentId = dto.PDepartmentId,
                PoliceAccountId = accountResult.AccountId,
                Department = department
            };

            await _context.Policemen.AddAsync(policeman);
            await _context.SaveChangesAsync();

            return new PolicemanCreatedDto
            {
                Id = policeman.Id,
                Email = dto.Email,
                TemporaryPassword = accountResult.TemporaryPassword
            };
        }
        public async Task DeletePolicemanAsync(Guid id)
        {
            var policeman = await _context.Policemen.FindAsync(id);
            if (policeman != null)
            {
                _context.Policemen.Remove(policeman);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<PoliceCar> CreatePoliceCarAsync(CreatePoliceCarDto dto)
        {
            var department = await _context.PoliceDepartments.FindAsync(dto.PDepartmentId);


            if (department == null)
            {
                throw new Exception("Komenda o podanym ID nie istnieje!");
            }

            var policeCar = new PoliceCar
            {
                LicensePlate = dto.LicensePlate,
                PDepartmentId = dto.PDepartmentId,
                PDepartment = department
            };
            await _context.PoliceCars.AddAsync(policeCar);
            await _context.SaveChangesAsync();
            return policeCar;

        }

        public async Task<IEnumerable<PDepartmentDto>> GetAllDepartmentsAsync()
        {
            return await _context.PoliceDepartments
                .Select(d => new PDepartmentDto
                {
                    Id = d.PDepartmentId,
                    Name = d.Name,
                    Address = d.Address,
                    District = d.District
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<PolicemanDto>> GetAllPolicemenAsync()
        {
            return await _context.Policemen
                .Select(p => new PolicemanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Surname = p.Surname,
                    BadgeNumber = p.BadgeNumber,
                    Rank = p.Rank,
                    PDepartmentId = p.PDepartmentId,
                    PoliceAccountId = p.PoliceAccountId
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<PoliceCarDto>> GetAllPoliceCarsAsync()
        {
            return await _context.PoliceCars
                .Select(c => new PoliceCarDto
                {
                    Id = c.Id,
                    LicensePlate = c.LicensePlate,
                    PDepartmentId = c.PDepartmentId
                })
                .ToListAsync();
        }

    } }
