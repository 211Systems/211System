using System.Text;
using System.Text.Json;
using _211system.Data;
using _211system.DTOs.Hospital;
using _211system.Models.Hospital;
using _211system.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using CPR112.Models;

namespace _211system.Services
{
    public class MedicalService : IMedicalService
    {
        private readonly _211DbContext _context;
        private readonly IAuthService _authService;
        private readonly IHttpClientFactory _httpClientFactory;

        public MedicalService(_211DbContext context, IAuthService authService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _authService = authService;
            _httpClientFactory = httpClientFactory;
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

        public async Task<AmbulanceDto> CreateAmbulanceAsync(CreateAmbulanceDto dto)
        {
            if (dto.ParamedicId.HasValue)
            {
                bool isAlreadyAssigned = await _context.Ambulances.AnyAsync(a => a.ParamedicId == dto.ParamedicId.Value);
                if (isAlreadyAssigned)
                {
                    throw new InvalidOperationException("Ten ratownik jest już przypisany do innej karetki.");
                }
            }

            var ambulance = new Ambulance
            {
                Type = dto.Type,
                LicensePlate = dto.LicensePlate,
                HospitalId = dto.HospitalId,
                ParamedicId = dto.ParamedicId,
                IsAvailable = true
            };

            await _context.Ambulances.AddAsync(ambulance);
            await _context.SaveChangesAsync();

            return new AmbulanceDto
            {
                Id = ambulance.Id,
                Type = ambulance.Type,
                LicensePlate = ambulance.LicensePlate,
                HospitalId = ambulance.HospitalId,
                IsAvailable = ambulance.IsAvailable,
                ParamedicId = ambulance.ParamedicId
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
                HospitalId = a.HospitalId,
                IsAvailable = a.IsAvailable,
                ParamedicId = a.ParamedicId
            });
        }
        
        public async Task<IEnumerable<AmbulanceDto>> GetAvailableAmbulancesAsync()
        {
            var available = await _context.Ambulances
                .Where(a => a.IsAvailable == true)
                .ToListAsync();

            return available.Select(a => new AmbulanceDto
            {
                Id = a.Id,
                Type = a.Type,
                LicensePlate = a.LicensePlate,
                HospitalId = a.HospitalId,
                IsAvailable = true,
                ParamedicId = a.ParamedicId
            });
        }
        
        public async Task AssignAmbulanceToIncidentAsync(Guid ambulanceId, Guid incidentId)
        {
            var ambulance = await _context.Ambulances
                .Include(a => a.Paramedic)
                .FirstOrDefaultAsync(a => a.Id == ambulanceId);

            if (ambulance == null) throw new ArgumentException("Karetka nie istnieje.");

            if (!ambulance.IsAvailable) 
                throw new InvalidOperationException("Ta karetka jest już w trakcie innej akcji.");
            
            ambulance.IsAvailable = false;
            ambulance.CurrentIncidentId = incidentId;

            var incident = await _context.Incidents.Include(i => i.SeverityLevel).FirstOrDefaultAsync(i => i.Id == incidentId);
            if (incident != null)
            {
                incident.IsMedicalActive = true; 
                
                if (incident.Status == "Nowe")
                {
                    incident.Status = "W toku";
                }
                _context.Incidents.Update(incident);
            }

            _context.Ambulances.Update(ambulance);

            if (ambulance.ParamedicId.HasValue)
            {
                var isBusy = await _context.MedicalOperations
                    .AnyAsync(m => m.ParamedicId == ambulance.ParamedicId.Value && m.EndTime == null);

                if (!isBusy)
                {
                    var operation = new MedicalOperation
                    {
                        ParamedicId = ambulance.ParamedicId.Value,
                        ReportId = incidentId,
                        StartTime = DateTime.UtcNow
                    };
                    await _context.MedicalOperations.AddAsync(operation);
                }
            }

            await _context.SaveChangesAsync();

            if (incident != null)
            {
                await NotifyMedicalEndpointAsync(ambulance, incident);
            }
        }
        
        private async Task NotifyMedicalEndpointAsync(Ambulance ambulance, Incident incident)
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
                    AssignedAmbulance = new 
                    {
                        AmbulanceId = ambulance.Id,
                        LicensePlate = ambulance.LicensePlate,
                        Type = ambulance.Type.ToString(),
                        HospitalId = ambulance.HospitalId,
                        Crew = ambulance.Paramedic != null ? new 
                        {
                            ParamedicId = ambulance.Paramedic.Id,
                            FirstName = ambulance.Paramedic.Name,
                            LastName = ambulance.Paramedic.LastName,
                            LicenseNumber = ambulance.Paramedic.LicenseNumber,
                            Rank = ambulance.Paramedic.Rank
                        } : null
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string targetEndpoint = "https://twoj-system.pl/api/medical-receptor/dispatch"; 

                var response = await client.PostAsync(targetEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Szpital nie odebrał powiadomienia! Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Awaria komunikacji ze szpitalem: {ex.Message}");
            }
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
            
            var ambulances = await _context.Ambulances
                .Where(a => a.CurrentIncidentId == operation.ReportId)
                .ToListAsync();
                
            foreach(var amb in ambulances)
            {
                amb.IsAvailable = true;
                amb.CurrentIncidentId = null;
                _context.Ambulances.Update(amb);
            }

            var incident = await _context.Incidents.FindAsync(operation.ReportId);
            if (incident != null)
            {
                incident.IsMedicalActive = false;
                
                if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
                {
                    incident.Status = "Zakończone";
                }
                
                _context.Incidents.Update(incident);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteHospitalAsync(Guid id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital != null)
            {
                _context.Hospitals.Remove(hospital);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteParamedicAsync(Guid id)
        {
            var paramedic = await _context.Paramedics.FindAsync(id);
            if (paramedic != null)
            {
                var operations = await _context.MedicalOperations.Where(o => o.ParamedicId == id).ToListAsync();
                if (operations.Any())
                {
                    _context.MedicalOperations.RemoveRange(operations);
                }

                var ambulances = await _context.Ambulances.Where(a => a.ParamedicId == id).ToListAsync();
                foreach (var amb in ambulances)
                {
                    amb.ParamedicId = null;
                    if (amb.CurrentIncidentId.HasValue)
                    {
                        amb.IsAvailable = true;
                        amb.CurrentIncidentId = null;
                    }
                }

                _context.Paramedics.Remove(paramedic);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAmbulanceAsync(Guid id, UpdateAmbulanceDto dto)
        {
            var ambulance = await _context.Ambulances.FindAsync(id);
            if (ambulance == null) throw new ArgumentException("Karetka nie istnieje.");

            if (dto.ParamedicId.HasValue)
            {
                bool isAlreadyAssigned = await _context.Ambulances.AnyAsync(a => a.ParamedicId == dto.ParamedicId.Value && a.Id != id);
                if (isAlreadyAssigned)
                {
                    throw new InvalidOperationException("Ten ratownik jest już przypisany do innej karetki.");
                }
            }

            ambulance.LicensePlate = dto.LicensePlate;
            ambulance.Type = dto.Type;
            ambulance.ParamedicId = dto.ParamedicId;

            _context.Ambulances.Update(ambulance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEquipmentAsync(Guid id)
        {
            var eq = await _context.AmbulanceEquipments.FindAsync(id);
            if (eq != null)
            {
                _context.AmbulanceEquipments.Remove(eq);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateHospitalAsync(Guid id, UpdateHospitalDto dto)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) throw new ArgumentException("Szpital nie istnieje.");

            hospital.Name = dto.Name;
            hospital.Address = dto.Address;
            hospital.HasSOR = dto.HasSOR;

            _context.Hospitals.Update(hospital);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAmbulanceAsync(Guid id)
        {
            var ambulance = await _context.Ambulances.FindAsync(id);
            if (ambulance != null)
            {
                if (ambulance.CurrentIncidentId.HasValue && ambulance.ParamedicId.HasValue)
                {
                    var activeOperations = await _context.MedicalOperations
                        .Where(o => o.ParamedicId == ambulance.ParamedicId.Value && o.ReportId == ambulance.CurrentIncidentId.Value)
                        .ToListAsync();
                        
                    if (activeOperations.Any())
                    {
                        _context.MedicalOperations.RemoveRange(activeOperations);
                    }

                    var incident = await _context.Incidents.FindAsync(ambulance.CurrentIncidentId.Value);
                    if (incident != null)
                    {
                        incident.IsMedicalActive = false;
                        if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
                        {
                            incident.Status = "Zakończone";
                        }
                        _context.Incidents.Update(incident);
                    }
                }

                _context.Ambulances.Remove(ambulance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AmbulanceEquipmentDto> AddEquipmentAsync(Guid ambulanceId, CreateAmbulanceEquipmentDto dto)
        {
            var equipment = new AmbulanceEquipment
            {
                Name = dto.Name,
                Quantity = dto.Quantity,
                AmbulanceId = ambulanceId
            };

            await _context.AmbulanceEquipments.AddAsync(equipment);
            await _context.SaveChangesAsync();

            return new AmbulanceEquipmentDto { Id = equipment.Id, Name = equipment.Name, Quantity = equipment.Quantity, AmbulanceId = equipment.AmbulanceId };
        }

        public async Task<IEnumerable<AmbulanceEquipmentDto>> GetEquipmentAsync(Guid ambulanceId)
        {
            var eq = await _context.AmbulanceEquipments.Where(e => e.AmbulanceId == ambulanceId).ToListAsync();
            return eq.Select(e => new AmbulanceEquipmentDto { Id = e.Id, Name = e.Name, Quantity = e.Quantity, AmbulanceId = e.AmbulanceId });
        }

        public async Task UpdateParamedicAsync(Guid id, UpdateParamedicDto dto)
        {
            var paramedic = await _context.Paramedics.FindAsync(id);
            if (paramedic == null) throw new ArgumentException("Pracownik nie istnieje w bazie.");

            paramedic.Name = dto.Name;
            paramedic.LastName = dto.LastName;
            paramedic.LicenseNumber = dto.LicenseNumber;
            paramedic.Rank = dto.Rank;

            _context.Paramedics.Update(paramedic);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MedicalOperationDto>> GetAllOperationsAsync()
        {
            var operations = await _context.MedicalOperations
                .Include(o => o.Paramedic)
                .OrderByDescending(o => o.StartTime)
                .ToListAsync();

            return operations.Select(o => new MedicalOperationDto
            {
                Id = o.Id,
                ParamedicId = o.ParamedicId ?? Guid.Empty,

                ParamedicName = o.Paramedic != null ? $"{o.Paramedic.Name} {o.Paramedic.LastName}" : "Nieznany Ratownik",
                ReportId = o.ReportId,
                StartTime = o.StartTime ?? DateTime.MinValue,

                EndTime = o.EndTime
            });
        }
    }
}