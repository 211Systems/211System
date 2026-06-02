using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.EntityFrameworkCore;
using Police;

namespace _211system.Models.Services
{
    public class PoliceService : IPoliceService
    {
        private readonly _211DbContext _context;
        private readonly IAuthService _authService;
        private readonly IHttpClientFactory _httpClientFactory;

        public PoliceService(_211DbContext context, IAuthService authService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _authService = authService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PDepartment> CreateDepartmentAsync(CreatePDepartmentDto dto)
        {
            var department = new PDepartment
            {
                Name = dto.Name,
                Address = dto.Address,
                District = dto.District,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                OperatingRadiusKm = dto.OperatingRadiusKm > 0 ? dto.OperatingRadiusKm : 15.0,
                HasHelipad = dto.HasHelipad
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
                Lastname = dto.Lastname,
                BadgeNumber = dto.BadgeNumber,
                Rank = dto.Rank,
                PDepartmentId = dto.PDepartmentId,
                PoliceAccountId = accountResult.AccountId,
                Department = department,
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
            if (policeman == null)
                return;

            if (await _context.PoliceCars.AnyAsync(c => c.PolicemanId == id))
                throw new InvalidOperationException(
                    "Nie można zwolnić policjanta - jest przypisany jako kierowca radiowozu. Najpierw usuń radiowóz lub edytuj radiowóz i odłącz kierowcę.");

            var operations = await _context.PoliceOperations.Where(o => o.PolicemanId == id).ToListAsync();
            if (operations.Any())
                _context.PoliceOperations.RemoveRange(operations);

            _context.Policemen.Remove(policeman);
            await _context.SaveChangesAsync();
        }

        public async Task<PoliceCar> CreatePoliceCarAsync(CreatePoliceCarDto dto)
        {
            var department = await _context.PoliceDepartments.FindAsync(dto.PDepartmentId);
            if (department == null) throw new Exception("Komenda o podanym ID nie istnieje!");

            if (dto.PolicemanId.HasValue)
            {
                bool isAlreadyAssigned = await _context.PoliceCars.AnyAsync(c => c.PolicemanId == dto.PolicemanId.Value);
                if (isAlreadyAssigned) throw new InvalidOperationException("Ten policjant jest już przypisany do innego radiowozu.");
            }

            var policeCar = new PoliceCar
            {
                LicensePlate = dto.LicensePlate,
                PDepartmentId = dto.PDepartmentId,
                PDepartment = department,
                PolicemanId = dto.PolicemanId,
                IsAvailable = true,
                Latitude = department.Latitude,
                Longitude = department.Longitude,
                Status = VehicleOperationalStatus.InBase
            };
            await _context.PoliceCars.AddAsync(policeCar);
            await _context.SaveChangesAsync();
            return policeCar;
        }

        public async Task AssignPoliceCarToIncidentAsync(Guid carId, Guid incidentId)
        {
            var car = await _context.PoliceCars
                .Include(c => c.Policeman)
                .FirstOrDefaultAsync(c => c.Id == carId);

            if (car == null) throw new ArgumentException("Radiowóz nie istnieje.");
            if (!car.IsAvailable) throw new InvalidOperationException("Ten radiowóz jest już w akcji.");

            car.IsAvailable = false;
            car.CurrentIncidentId = incidentId;

            var incident = await _context.Incidents.Include(i => i.SeverityLevel).FirstOrDefaultAsync(i => i.Id == incidentId);
            if (incident != null)
            {
                incident.IsPoliceActive = true;
                if (incident.Status == "Nowe") incident.Status = "W toku";
                _context.Incidents.Update(incident);
            }

            _context.PoliceCars.Update(car);

            if (car.PolicemanId.HasValue)
            {
                var isBusy = await _context.PoliceOperations
                    .AnyAsync(o => o.PolicemanId == car.PolicemanId.Value && o.EndTime == null);

                if (!isBusy)
                {
                    Guid deptId = car.PDepartmentId;
                    if (deptId == Guid.Empty && car.Policeman != null)
                    {
                        deptId = car.Policeman.PDepartmentId;
                    }

                    var operation = new PoliceOperation
                    {
                        PolicemanId = car.PolicemanId.Value,
                        IncidentId = incidentId,
                        PDepartmentId = deptId,
                        StartTime = DateTime.UtcNow
                    };
                    await _context.PoliceOperations.AddAsync(operation);
                }
            }

            await _context.SaveChangesAsync();

            if (incident != null)
            {
                await NotifyPoliceEndpointAsync(car, incident);
            }
        }

        private async Task NotifyPoliceEndpointAsync(PoliceCar car, CPR112.Models.Incident incident)
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
                        CarId = car.Id,
                        LicensePlate = car.LicensePlate,
                        DepartmentId = car.PDepartmentId,
                        Crew = car.Policeman != null ? new
                        {
                            PolicemanId = car.Policeman.Id,
                            FirstName = car.Policeman.Name,
                            LastName = car.Policeman.Lastname,
                            BadgeNumber = car.Policeman.BadgeNumber,
                            Rank = car.Policeman.Rank
                        } : null
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                string targetEndpoint = "https://system-policji.pl/api/dispatch/receive";
                await client.PostAsync(targetEndpoint, content);
            }
            catch { }
        }

        public async Task<IEnumerable<PDepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _context.PoliceDepartments.ToListAsync();
            var airbases = await _context.Airbases
                .Where(a => a.ServiceType == ServiceType.Police)
                .ToListAsync();

            return departments.Select(d => new PDepartmentDto
            {
                Id = d.PDepartmentId,
                Name = d.Name,
                Address = d.Address,
                District = d.District,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                OperatingRadiusKm = d.OperatingRadiusKm,
                HasHelipad = HelipadHelper.ResolveHasHelipad(d.HasHelipad, d.Latitude, d.Longitude, ServiceType.Police, airbases)
            });
        }

        public async Task<IEnumerable<PolicemanDto>> GetAllPolicemenAsync()
        {
            var policemen = await _context.Policemen
                .Include(p => p.PoliceAccount)
                .ToListAsync();

            return await _context.Policemen
                .Select(p => new PolicemanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Surname = p.Lastname,
                    BadgeNumber = p.BadgeNumber,
                    Rank = p.Rank,
                    PDepartmentId = p.PDepartmentId,
                    PoliceAccountId = p.PoliceAccountId,
                    Email = p.PoliceAccount != null ? p.PoliceAccount.Email : "Brak"
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<PoliceCarDto>> GetAllPoliceCarsAsync()
        {
            var cars = await _context.PoliceCars
                .Include(c => c.PDepartment)
                .ToListAsync();

            return cars.Select(c => new PoliceCarDto
            {
                Id = c.Id,
                LicensePlate = c.LicensePlate,
                PDepartmentId = c.PDepartmentId,
                IsAvailable = c.IsAvailable,
                PolicemanId = c.PolicemanId,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Status = (int)c.Status
            });
        }

        public async Task UpdatePoliceCarAsync(Guid id, UpdatePoliceCarDto dto)
        {
            var car = await _context.PoliceCars.FindAsync(id);
            if (car == null) throw new ArgumentException("Radiowóz nie istnieje.");

            if (dto.PolicemanId.HasValue)
            {
                bool isAlreadyAssigned = await _context.PoliceCars.AnyAsync(c => c.PolicemanId == dto.PolicemanId.Value && c.Id != id);
                if (isAlreadyAssigned) throw new InvalidOperationException("Ten policjant jest już przypisany do innego radiowozu.");
            }

            car.LicensePlate = dto.LicensePlate;
            car.PolicemanId = dto.PolicemanId;

            _context.PoliceCars.Update(car);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePoliceCarAsync(Guid id)
        {
            var car = await _context.PoliceCars.FindAsync(id);
            if (car == null) return;

            if (!car.IsAvailable || car.CurrentIncidentId.HasValue)
                throw new InvalidOperationException("Nie można wyrejestrować pojazdu - jest w akcji. Najpierw zakończ operację lub zwolnij pojazd z zgłoszenia.");
            if (car != null)
            {
                if (car.CurrentIncidentId.HasValue && car.PolicemanId.HasValue)
                {
                    var activeOperations = await _context.PoliceOperations
                        .Where(o => o.PolicemanId == car.PolicemanId.Value && o.IncidentId == car.CurrentIncidentId.Value)
                        .ToListAsync();

                    if (activeOperations.Any()) _context.PoliceOperations.RemoveRange(activeOperations);

                    var incident = await _context.Incidents.FindAsync(car.CurrentIncidentId.Value);
                    if (incident != null)
                    {
                        incident.IsPoliceActive = false;
                        if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive) incident.Status = "Zakończone";
                        _context.Incidents.Update(incident);
                    }
                }

                _context.PoliceCars.Remove(car);
                await _context.SaveChangesAsync();
            }
        }

        public async Task TransportToStationAsync(Guid operationId, Guid departmentId)
        {
            var operation = await _context.PoliceOperations.FindAsync(operationId);
            if (operation == null) throw new ArgumentException("Operacja nie istnieje.");

            var car = await _context.PoliceCars.FirstOrDefaultAsync(c => c.CurrentIncidentId == operation.IncidentId && c.PolicemanId == operation.PolicemanId);
            if (car != null)
            {
                car.Status = VehicleOperationalStatus.Transporting;
                _context.PoliceCars.Update(car);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReturnToBaseAsync(Guid operationId)
        {
            var operation = await _context.PoliceOperations.FindAsync(operationId);
            if (operation == null) throw new ArgumentException("Operacja nie istnieje.");

            var car = await _context.PoliceCars.FirstOrDefaultAsync(c => c.CurrentIncidentId == operation.IncidentId && c.PolicemanId == operation.PolicemanId);
            if (car != null)
            {
                car.Status = VehicleOperationalStatus.ReturningToBase;
                _context.PoliceCars.Update(car);
            }

            var incident = await _context.Incidents.FindAsync(operation.IncidentId);
            if (incident != null)
            {
                incident.IsPoliceActive = false;
                if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
                {
                    incident.Status = "Zakończone";
                }
                _context.Incidents.Update(incident);
            }

            await _context.SaveChangesAsync();
        }
    }
}