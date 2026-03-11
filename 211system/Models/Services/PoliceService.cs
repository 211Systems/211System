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

        public PoliceService(_211DbContext context)
        {
            _context = context;
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

        public async Task<Policeman> CreatePolicemanAsync(CreatePolicemanDto dto)
        {
            var department = await _context.PoliceDepartments.FindAsync(dto.PDepartmentId);

            if (department == null)
            {
                throw new Exception("Komenda o podanym ID nie istnieje!");
            }

            var policeman = new Policeman
            {
                Name = dto.Name,
                Surname = dto.Surname,
                BadgeNumber = dto.BadgeNumber,
                Rank = dto.Rank,
                PDepartmentId = dto.PDepartmentId,
                PoliceAccountId = dto.PoliceAccountId,
                Department = department
            };

            await _context.Policemen.AddAsync(policeman);
            await _context.SaveChangesAsync();

            return policeman;
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
