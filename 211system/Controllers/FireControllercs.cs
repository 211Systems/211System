using System;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using FireDepartment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using static _211system.Controllers.PoliceController;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FireController : Controller
    {
        private readonly IFireService _fireService;
        private readonly _211DbContext _context;

        public FireController(IFireService fireService, _211DbContext context)
        {
            _fireService = fireService;
            _context = context;

        }

        [Authorize(Roles = "Naczelnik, Admin")]
        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateFDepartmentDto dto)
        {
            var result = await _fireService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin")]
        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var result = await _fireService.GetAllDepartmentsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPost("firemen")]
        public async Task<IActionResult> AddFireman([FromBody] CreateFiremanDto dto)
        {
            var nameRegex = new Regex(@"^[a-zA-ZĄĆĘŁŃÓŚŹŻąćęłńóśźż\s\-]{2,50}$");
            if (string.IsNullOrWhiteSpace(dto.Name) || !nameRegex.IsMatch(dto.Name))
                return BadRequest(new { message = "Nieprawidłowe imię (tylko litery, 2-50 znaków)." });
            if (string.IsNullOrWhiteSpace(dto.Lastname) || !nameRegex.IsMatch(dto.Lastname))
                return BadRequest(new { message = "Nieprawidłowe nazwisko (tylko litery, 2-50 znaków)." });

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                return BadRequest(new { message = "Ten adres e-mail jest już zajęty!" });

            try
            {
                var result = await _fireService.CreateFiremanAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin")]
        [HttpGet("firemen")]
        public async Task<IActionResult> GetAllFiremen()
        {
            var result = await _fireService.GetAllFiremenAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpDelete("firemen/{id}")]
        public async Task<IActionResult> DeleteFireman(Guid id)
        {
            await _fireService.DeleteFiremanAsync(id);
            return Ok(new { message = "Zwolniono strażaka." });
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPost("firetrucks")]
        public async Task<IActionResult> AddFireTruck([FromBody] CreateFireTruckDto dto)
        {
            try
            {
                var result = await _fireService.CreateFireTruckAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin, Admin112, Dyspozytor112")]
        [HttpGet("firetrucks")]
        public async Task<IActionResult> GetAllFireTrucks()
        {
            var result = await _fireService.GetAllFireTrucksAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPut("firetrucks/{id}")]
        public async Task<IActionResult> UpdateFireTruck(Guid id, [FromBody] UpdateFireTruckDto dto)
        {
            try
            {
                await _fireService.UpdateFireTruckAsync(id, dto);
                return Ok(new { message = "Zaktualizowano wóz strażacki." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpDelete("firetrucks/{id}")]
        public async Task<IActionResult> DeleteFireTruck(Guid id)
        {
            await _fireService.DeleteFireTruckAsync(id);
            return Ok(new { message = "Usunięto wóz strażacki." });
        }

        [HttpPut("firetrucks/{truckId}/assign/{incidentId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Naczelnik, Kapitan")]
        public async Task<IActionResult> AssignFireTruckToIncident(Guid truckId, Guid incidentId)
        {
            try
            {
                await _fireService.AssignFireTruckToIncidentAsync(truckId, incidentId);

                return Ok(new { message = "Wóz strażacki został zadysponowany do akcji!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("operations")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetOperations()
        {
            var operations = await _context.FireOperations
                .Include(o => o.Fireman)
                .ToListAsync();

            var result = operations.Select(op => new
            {
                Id = op.Id,
                StartTime = op.StartTime,
                EndTime = op.EndTime,
                FiremanId = op.FiremanId,
                FiremanName = op.Fireman != null ? $"{op.Fireman.Name} {op.Fireman.Lastname}" : "Brak Danych",
                ReportId = op.IncidentId
            });

            return Ok(result);
        }

        [HttpPost("operations/start")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak, Admin112, Dyspozytor112")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid firemanId, [FromQuery] Guid reportId)
        {
            try
            {
                var truck = await _context.FireTrucks.FirstOrDefaultAsync(t => t.FiremanId == firemanId);
                if (truck != null)
                {
                    truck.IsAvailable = false;
                    truck.CurrentIncidentId = reportId;
                    _context.FireTrucks.Update(truck);
                }

                var incident = await _context.Incidents.FindAsync(reportId);
                if (incident != null)
                {
                    incident.IsFireActive = true;

                    if (incident.Status != "W toku")
                    {
                        var oldStatus = incident.Status;
                        incident.Status = "W toku";
                        _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                        {
                            IncidentId = incident.Id,
                            OldStatus = oldStatus,
                            NewStatus = "W toku",
                            ChangedAt = DateTime.UtcNow
                        });
                    }
                    _context.Incidents.Update(incident);
                }

                var operation = new FireDepartmentOperation
                {
                    FiremanId = firemanId,
                    IncidentId = reportId,
                    StartTime = DateTime.UtcNow
                };

                _context.FireOperations.Add(operation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wóz strażacki w drodze na miejsce zdarzenia!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd podczas dysponowania straży: " + ex.Message });
            }
        }


        [HttpPut("operations/{id}/end")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak")]
        public async Task<IActionResult> EndOperation(Guid id)
        {
            var operation = await _context.FireOperations.FindAsync(id);
            if (operation == null) return NotFound();

            operation.EndTime = DateTime.UtcNow;

            var truck = await _context.FireTrucks
                .FirstOrDefaultAsync(t => t.FiremanId == operation.FiremanId && t.CurrentIncidentId == operation.IncidentId);

            if (truck != null)
            {
                truck.IsAvailable = true;
                truck.CurrentIncidentId = null;
            }

            var incident = await _context.Incidents.FindAsync(operation.IncidentId);
            if (incident != null)
            {
                incident.IsFireActive = false;

                if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
                {
                    if (incident.Status != "Zakończone")
                    {
                        var oldStatus = incident.Status;
                        incident.Status = "Zakończone";
                        _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                        {
                            IncidentId = incident.Id,
                            OldStatus = oldStatus,
                            NewStatus = "Zakończone",
                            ChangedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                    {
                        IncidentId = incident.Id,
                        OldStatus = incident.Status,
                        NewStatus = "Wóz PSP powrócił do bazy",
                        ChangedAt = DateTime.UtcNow
                    });
                }

                _context.Incidents.Update(incident);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Akcja ratownicza zakończona. Wóz wraca do remizy." });
        }

        [HttpGet("incidents/{id}")]
        [Authorize]
        public async Task<IActionResult> GetIncidentDetails(Guid id)
        {
            var incident = await _context.Incidents
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    incidentNumber = i.IncidentNumber,
                    description = i.Description,
                    severity = i.SeverityLevel != null ? i.SeverityLevel.Name : "Brak",
                    incidentType = i.IncidentType != null ? i.IncidentType.Name : "Brak Typu",
                    status = i.Status,
                    address = i.Latitude != 0 && i.Longitude != 0 ? $"GPS: {i.Latitude}, {i.Longitude}" : "Nieznana",
                    reportDate = i.ReportDate
                })
                .FirstOrDefaultAsync();

            if (incident == null) return NotFound();
            return Ok(incident);
        }

        [HttpPut("departments/{id}")]
        [Authorize(Roles = "Admin, Naczelnik")]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] CreateFDepartmentDto dto)
        {
            var dept = await _context.FireDepartments.FindAsync(id);
            if (dept == null) return NotFound();

            dept.Name = dto.Name;
            dept.Address = dto.Address;
            dept.District = dto.District;
            dept.HasHelipad = dto.HasHelipad;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano placówkę." });
        }

        [HttpDelete("departments/{id}")]
        [Authorize(Roles = "Admin, Naczelnik")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var dept = await _context.FireDepartments.FindAsync(id);
            if (dept == null) return NotFound(new { message = "Nie znaleziono placówki." });

            try
            {
                _context.FireDepartments.Remove(dept);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Usunięto remizę strażacką." });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Nie można usunąć placówki. Najpierw usuń lub przenieś przypisanych do niej strażaków oraz sprzęt bojowy (wozy strażackie)." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("firemen/{id}")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan")]
        public async Task<IActionResult> UpdateFireman(Guid id, [FromBody] CreateFiremanDto dto)
        {
            var fireman = await _context.Firemen.FindAsync(id);
            if (fireman == null) return NotFound();

            fireman.Name = dto.Name;
            fireman.Lastname = dto.Lastname;
            fireman.BadgeNumber = dto.BadgeNumber;
            fireman.Rank = dto.Rank;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano dane strażaka." });
        }
        [HttpPost("firetrucks/{truckId}/equipment")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan")]
        public async Task<IActionResult> AddTruckEquipment(Guid truckId, [FromBody] EquipmentDto dto)
        {
            var eq = new FireEquipment { FireTruckId = truckId, Name = dto.Name, Quantity = dto.Quantity };
            _context.FireEquipments.Add(eq);
            await _context.SaveChangesAsync();
            return Ok(eq);
        }

        [HttpGet("firetrucks/{truckId}/equipment")]
        [Authorize]
        public async Task<IActionResult> GetTruckEquipment(Guid truckId)
        {
            var equipment = await _context.FireEquipments.Where(e => e.FireTruckId == truckId).ToListAsync();
            return Ok(equipment);
        }

        [HttpDelete("equipment/{id}")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan")]
        public async Task<IActionResult> DeleteTruckEquipment(Guid id)
        {
            var eq = await _context.FireEquipments.FindAsync(id);
            if (eq != null) { _context.FireEquipments.Remove(eq); await _context.SaveChangesAsync(); }
            return Ok();
        }

        [HttpPut("firetrucks/{id}/location")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Strazak, Admin112, Dyspozytor112")]
        public async Task<IActionResult> UpdateFireTruckLocation(Guid id, [FromBody] UpdateLocationDto dto)
        {
            try
            {
                var truck = await _context.FireTrucks.FindAsync(id);
                if (truck == null) return NotFound(new { message = "Wóz strażacki o podanym ID nie istnieje." });

                truck.Latitude = dto.Latitude;
                truck.Longitude = dto.Longitude;

                if (dto.Status.HasValue)
                {
                    truck.Status = (VehicleOperationalStatus)dto.Status.Value;
                }

                _context.FireTrucks.Update(truck);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Pozycja wozu strażackiego została zaktualizowana." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd podczas aktualizacji GPS: " + ex.Message });
            }
        }

        [HttpPost("operations/{id}/return")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak, Admin112, Dyspozytor112")]
        public async Task<IActionResult> ReturnToBase(Guid id)
        {
            await _fireService.ReturnToBaseAsync(id);
            return Ok(new { message = "Wóz wraca do remizy." });
        }

        [HttpPost("firetrucks/{id}/free")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak, Admin112, Dyspozytor112")]
        public async Task<IActionResult> FreeFireTruck(Guid id)
        {
            try
            {
                var truck = await _context.FireTrucks.FindAsync(id);
                if (truck == null) return NotFound(new { message = "Nie znaleziono pojazdu PSP." });

                truck.IsAvailable = true;
                truck.Status = VehicleOperationalStatus.InBase;

                if (truck.CurrentIncidentId.HasValue)
                {
                    var incidentId = truck.CurrentIncidentId.Value;

                    var query = _context.FireOperations
                        .Where(o => o.IncidentId == incidentId && o.EndTime == null);

                    if (truck.FiremanId.HasValue)
                        query = query.Where(o => o.FiremanId == truck.FiremanId || o.FiremanId == null);
                    else
                        query = query.Where(o => o.FDepartmentId == truck.FDepartmentId);

                    var openOps = await query.ToListAsync();
                    foreach (var op in openOps)
                    {
                        op.EndTime = DateTime.UtcNow;
                        _context.FireOperations.Update(op);
                    }

                    truck.CurrentIncidentId = null;

                    var incident = await _context.Incidents.FindAsync(incidentId);
                    if (incident != null)
                    {
                        incident.IsFireActive = false;

                        if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive
                            && incident.Status != "Zakończone")
                        {
                            var oldStatus = incident.Status;
                            incident.Status = "Zakończone";
                            _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                            {
                                IncidentId = incident.Id,
                                OldStatus = oldStatus,
                                NewStatus = "Zakończone",
                                ChangedAt = DateTime.UtcNow
                            });
                        }

                        _context.Incidents.Update(incident);
                    }
                }

                _context.FireTrucks.Update(truck);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Pojazd PSP zwolniony." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd zwalniania pojazdu PSP: " + ex.Message });
            }
        }
    }
}