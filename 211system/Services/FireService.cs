using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public FireService(_211DbContext context, IAuthService authService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _authService = authService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<FDepartment> CreateDepartmentAsync(CreateFDepartmentDto dto)
        {
            var department = new FDepartment
            {
                Name = dto.Name,
                Address = dto.Address,
                District = dto.District,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                OperatingRadiusKm = dto.OperatingRadiusKm > 0 ? dto.OperatingRadiusKm : 15.0
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
                Lastname = dto.Lastname,
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
                var operations = await _context.FireOperations.Where(o => o.FiremanId == id).ToListAsync();
                if (operations.Any())
                {
                    _context.FireOperations.RemoveRange(operations);
                }

                var trucks = await _context.FireTrucks.Where(t => t.FiremanId == id).ToListAsync();
                foreach (var truck in trucks)
                {
                    truck.FiremanId = null;
                    if (truck.CurrentIncidentId.HasValue)
                    {
                        truck.IsAvailable = true;
                        truck.CurrentIncidentId = null;
                    }
                }

                _context.Firemen.Remove(fireman);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<FireTruck> CreateFireTruckAsync(CreateFireTruckDto dto)
        {
            var department = await _context.FireDepartments.FindAsync(dto.FDepartmentId);
            if (department == null) throw new Exception("Remiza o podanym ID nie istnieje!");

            if (dto.FiremanId.HasValue)
            {
                bool isAlreadyAssigned = await _context.FireTrucks.AnyAsync(t => t.FiremanId == dto.FiremanId.Value);
                if (isAlreadyAssigned) throw new InvalidOperationException("Ten strażak jest już przypisany do innego wozu.");
            }

            var fireTruck = new FireTruck
            {
                LicensePlate = dto.LicensePlate,
                FDepartmentId = dto.FDepartmentId,
                Department = department,
                FiremanId = dto.FiremanId,
                IsAvailable = true
            };

            await _context.FireTrucks.AddAsync(fireTruck);
            await _context.SaveChangesAsync();
            return fireTruck;
        }

        public async Task AssignFireTruckToIncidentAsync(Guid truckId, Guid incidentId)
        {
            var truck = await _context.FireTrucks
                .Include(t => t.Fireman)
                .FirstOrDefaultAsync(t => t.Id == truckId);

            if (truck == null) throw new ArgumentException("Wóz strażacki nie istnieje.");
            if (!truck.IsAvailable) throw new InvalidOperationException("Ten wóz jest już w akcji.");

            truck.IsAvailable = false;
            truck.CurrentIncidentId = incidentId;

            var incident = await _context.Incidents.Include(i => i.SeverityLevel).FirstOrDefaultAsync(i => i.Id == incidentId);
            if (incident != null)
            {
                incident.IsFireActive = true;
                if (incident.Status == "Nowe") incident.Status = "W toku";
                _context.Incidents.Update(incident);
            }

            _context.FireTrucks.Update(truck);

            if (truck.FiremanId.HasValue)
            {
                var isBusy = await _context.FireOperations
                    .AnyAsync(o => o.FiremanId == truck.FiremanId.Value && o.EndTime == null);

                if (!isBusy)
                {
                    Guid deptId = truck.FDepartmentId;
                    if (deptId == Guid.Empty && truck.Fireman != null)
                    {
                        deptId = truck.Fireman.FDepartmentId;
                    }

                    var operation = new FireDepartmentOperation
                    {
                        FiremanId = truck.FiremanId.Value,
                        IncidentId = incidentId,
                        FDepartmentId = deptId,
                        StartTime = DateTime.UtcNow
                    };
                    await _context.FireOperations.AddAsync(operation);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyFireEndpointAsync(FireTruck truck, CPR112.Models.Incident incident)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    IncidentId = incident.Id,
                    IncidentNumber = incident.IncidentNumber ?? "Brak",
                    Description = incident.Description,
                    Severity = incident.SeverityLevel != null ? incident.SeverityLevel.Name : "Brak",
                    DispatchTime = DateTime.UtcNow,
                    AssignedUnit = new
                    {
                        TruckId = truck.Id,
                        LicensePlate = truck.LicensePlate,
                        DepartmentId = truck.FDepartmentId,
                        Crew = truck.Fireman != null ? new
                        {
                            FiremanId = truck.Fireman.Id,
                            FirstName = truck.Fireman.Name,
                            LastName = truck.Fireman.Lastname,
                            BadgeNumber = truck.Fireman.BadgeNumber,
                            Rank = truck.Fireman.Rank
                        } : null
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                string targetEndpoint = "https://straz-pozarna.pl/api/dispatch/receive";
                await client.PostAsync(targetEndpoint, content);
            }
            catch { }
        }

        public async Task<IEnumerable<FDepartmentDto>> GetAllDepartmentsAsync()
        {
            return await _context.FireDepartments
                .Select(d => new FDepartmentDto
                {
                    Id = d.FDepartmentId,
                    Name = d.Name,
                    Address = d.Address,
                    District = d.District,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    OperatingRadiusKm = d.OperatingRadiusKm
                }).ToListAsync();
        }

        public async Task<IEnumerable<FiremanDto>> GetAllFiremenAsync()
        {
            return await _context.Firemen
                .Select(f => new FiremanDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Surname = f.Lastname,
                    BadgeNumber = f.BadgeNumber,
                    Rank = f.Rank,
                    FDepartmentId = f.FDepartmentId,
                    FireAccountId = f.FireAccountId
                }).ToListAsync();
        }

        public async Task<IEnumerable<FireTruckDto>> GetAllFireTrucksAsync()
        {
            var trucks = await _context.FireTrucks
                .Include(t => t.Department)
                .ToListAsync();

            return trucks.Select(t => new FireTruckDto
            {
                Id = t.Id,
                LicensePlate = t.LicensePlate,
                FDepartmentId = t.FDepartmentId,
                IsAvailable = t.IsAvailable,
                FiremanId = t.FiremanId,
                Latitude = t.Department?.Latitude ?? 0,
                Longitude = t.Department?.Longitude ?? 0
            });
        }

        public async Task UpdateFireTruckAsync(Guid id, UpdateFireTruckDto dto)
        {
            var truck = await _context.FireTrucks.FindAsync(id);
            if (truck == null) throw new ArgumentException("Wóz strażacki nie istnieje.");

            if (dto.FiremanId.HasValue)
            {
                bool isAlreadyAssigned = await _context.FireTrucks.AnyAsync(t => t.FiremanId == dto.FiremanId.Value && t.Id != id);
                if (isAlreadyAssigned) throw new InvalidOperationException("Ten strażak jest już przypisany do innego wozu.");
            }

            truck.LicensePlate = dto.LicensePlate;
            truck.FiremanId = dto.FiremanId;

            _context.FireTrucks.Update(truck);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFireTruckAsync(Guid id)
        {
            var truck = await _context.FireTrucks.FindAsync(id);
            if (truck != null)
            {
                if (truck.CurrentIncidentId.HasValue && truck.FiremanId.HasValue)
                {
                    var activeOperations = await _context.FireOperations
                        .Where(o => o.FiremanId == truck.FiremanId.Value && o.IncidentId == truck.CurrentIncidentId.Value)
                        .ToListAsync();

                    if (activeOperations.Any()) _context.FireOperations.RemoveRange(activeOperations);

                    var incident = await _context.Incidents.FindAsync(truck.CurrentIncidentId.Value);
                    if (incident != null)
                    {
                        incident.IsFireActive = false;
                        if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive) incident.Status = "Zakończone";
                        _context.Incidents.Update(incident);
                    }
                }

                _context.FireTrucks.Remove(truck);
                await _context.SaveChangesAsync();
            }
        }
    }
}