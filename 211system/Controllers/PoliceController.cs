using _211system.Data;
using _211system.Models;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Police;


namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliceController : Controller
    {
        private readonly IPoliceService _policeService;
        private readonly _211DbContext _context;

        public PoliceController(IPoliceService policeService, _211DbContext context)
        {
            _policeService = policeService;
            _context = context;
        }

        [Authorize(Roles = "Inspektor, Admin")]
        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreatePDepartmentDto dto)
        {
            var result = await _policeService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Inspektor, Komendant, Policjant")]
        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _policeService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPost("policemen")]
        public async Task<IActionResult> AddPoliceman([FromBody] CreatePolicemanDto dto)
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
                var result = await _policeService.CreatePolicemanAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Inspektor, Komendant, Policjant")]
        [HttpGet("policemen")]
        public async Task<IActionResult> GetAllPolicemen()
        {
            var policemen = await _policeService.GetAllPolicemenAsync();
            return Ok(policemen);
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpDelete("policemen/{id}")]
        public async Task<IActionResult> DeletePoliceman(Guid id)
        {
            try
            {
                await _policeService.DeletePolicemanAsync(id);
                return Ok(new { message = "Zwolniono policjanta." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPost("cars")]
        public async Task<IActionResult> AddPoliceCar([FromBody] CreatePoliceCarDto dto)
        {
            try
            {
                var result = await _policeService.CreatePoliceCarAsync(dto);
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

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPut("cars/{id}")]
        public async Task<IActionResult> UpdatePoliceCar(Guid id, [FromBody] UpdatePoliceCarDto dto)
        {
            try
            {
                await _policeService.UpdatePoliceCarAsync(id, dto);
                return Ok(new { message = "Zaktualizowano radiowóz." });
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

        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Komendant, Inspektor")]
        [HttpPut("cars/{carId}/assign/{incidentId}")]
        public async Task<IActionResult> AssignPoliceCarToIncident(Guid carId, Guid incidentId)
        {
            try
            {
                await _policeService.AssignPoliceCarToIncidentAsync(carId, incidentId);
                return Ok(new { message = "Radiowóz został zadysponowany do zgłoszenia." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("cars")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAllCars()
        {
            var cars = await _policeService.GetAllPoliceCarsAsync();
            return Ok(cars);
        }

        [HttpDelete("cars/{id}")]
        [Authorize(Roles = "Admin, Komendant, Inspektor")]
        public async Task<IActionResult> DeleteCar(Guid id)
        {
            try
            {
                await _policeService.DeletePoliceCarAsync(id);
                return Ok(new { message = "Usunięto radiowóz." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("operations")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetOperations()
        {
            var operations = await _context.PoliceOperations
                .Include(o => o.Policeman)
                .Select(o => new 
                {
                    Id = o.Id,
                    StartTime = o.StartTime,
                    EndTime = o.EndTime,
                    IncidentId = o.IncidentId,
                    PolicemanId = o.PolicemanId,
                    PolicemanName = o.Policeman != null ? (o.Policeman.Name + " " + o.Policeman.Lastname) : "Brak Danych"
                })
                .ToListAsync();
                
            return Ok(operations);
        }

        [HttpPost("operations/start")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid policemanId, [FromQuery] Guid reportId)
        {
            var car = await _context.PoliceCars.FirstOrDefaultAsync(c => c.PolicemanId == policemanId);
            if (car == null) return BadRequest(new { message = "Błąd: Ten policjant nie jest aktualnie przypisany do żadnego radiowozu!" });

            try
            {
                await _policeService.AssignPoliceCarToIncidentAsync(car.Id, reportId);

                var incident = await _context.Incidents.FindAsync(reportId);
                if (incident != null && incident.Status != "W toku")
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
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Radiowóz został zadysponowany i jest w drodze!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("operations/{id}/end")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant")]
        public async Task<IActionResult> EndOperation(Guid id)
        {
            var operation = await _context.PoliceOperations.FindAsync(id);
            if (operation == null) return NotFound();

            operation.EndTime = DateTime.UtcNow;

            var car = await _context.PoliceCars
                .FirstOrDefaultAsync(c => c.PolicemanId == operation.PolicemanId && c.CurrentIncidentId == operation.IncidentId);

            if (car != null)
            {
                car.IsAvailable = true;
                car.CurrentIncidentId = null;
            }

            var incident = await _context.Incidents.FindAsync(operation.IncidentId);
            if (incident != null)
            {
                incident.IsPoliceActive = false;

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
                        NewStatus = "Radiowóz zakończył działania",
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Interwencja zakończona. Radiowóz wraca do bazy." });
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
                    Address = $"GPS: {i.Latitude}, {i.Longitude}",
                    reportDate = i.ReportDate
                })
                .FirstOrDefaultAsync();

            if (incident == null) return NotFound();
            return Ok(incident);
        }
        [HttpPut("departments/{id}")]
        [Authorize(Roles = "Admin, Inspektor")]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] CreatePDepartmentDto dto)
        {
            var dept = await _context.PoliceDepartments.FindAsync(id);
            if (dept == null) return NotFound();

            dept.Name = dto.Name;
            dept.Address = dto.Address;
            dept.District = dto.District;
            dept.HasHelipad = dto.HasHelipad;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano placówkę." });
        }
        [HttpDelete("departments/{id}")]
        [Authorize(Roles = "Admin, Inspektor")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var dept = await _context.PoliceDepartments.FindAsync(id);
            if (dept == null) return NotFound(new { message = "Nie znaleziono placówki." });

            var hasDependencies = await _context.Policemen.AnyAsync(p => p.PDepartmentId == id)
                || await _context.PoliceCars.AnyAsync(c => c.PDepartmentId == id);

            if (hasDependencies)
            {
                return BadRequest(new { message = "Nie można usunąć placówki. Najpierw usuń lub przenieś przypisanych do niej funkcjonariuszy oraz sprzęt (radiowozy)." });
            }

            try
            {
                _context.PoliceDepartments.Remove(dept);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Usunięto placówkę pomyślnie." });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Nie można usunąć placówki. Najpierw usuń lub przenieś przypisanych do niej funkcjonariuszy oraz sprzęt (radiowozy)." });
            }
        }

        [HttpPut("policemen/{id}")]
        [Authorize(Roles = "Admin, Inspektor, Komendant")]
        public async Task<IActionResult> UpdatePoliceman(Guid id, [FromBody] CreatePolicemanDto dto)
        {
            var policeman = await _context.Policemen.FindAsync(id);
            if (policeman == null) return NotFound();

            policeman.Name = dto.Name;
            policeman.Lastname = dto.Lastname;
            policeman.BadgeNumber = dto.BadgeNumber;
            policeman.Rank = dto.Rank;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano dane funkcjonariusza." });
        }
        public class EquipmentDto { public string Name { get; set; } public int Quantity { get; set; } }

        [HttpPost("cars/{carId}/equipment")]
        [Authorize(Roles = "Admin, Inspektor, Komendant")]
        public async Task<IActionResult> AddCarEquipment(Guid carId, [FromBody] EquipmentDto dto)
        {
            var eq = new PoliceEquipment { PoliceCarId = carId, Name = dto.Name, Quantity = dto.Quantity };
            _context.PoliceEquipments.Add(eq);
            await _context.SaveChangesAsync();
            return Ok(eq);
        }

        [HttpGet("cars/{carId}/equipment")]
        [Authorize]
        public async Task<IActionResult> GetCarEquipment(Guid carId)
        {
            var equipment = await _context.PoliceEquipments.Where(e => e.PoliceCarId == carId).ToListAsync();
            return Ok(equipment);
        }

        [HttpDelete("equipment/{id}")]
        [Authorize(Roles = "Admin, Inspektor, Komendant")]
        public async Task<IActionResult> DeleteCarEquipment(Guid id)
        {
            var eq = await _context.PoliceEquipments.FindAsync(id);
            if (eq != null) { _context.PoliceEquipments.Remove(eq); await _context.SaveChangesAsync(); }
            return Ok();
        }

        [HttpPut("cars/{id}/location")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Admin112, Dyspozytor112")]
        public async Task<IActionResult> UpdatePoliceCarLocation(Guid id, [FromBody] UpdateLocationDto dto)
        {
            var car = await _context.PoliceCars.FindAsync(id);
            if (car == null) return NotFound();

            car.Latitude = dto.Latitude;
            car.Longitude = dto.Longitude;

            if (dto.Status.HasValue)
            {
                car.Status = (VehicleOperationalStatus)dto.Status.Value;
            }

            _context.PoliceCars.Update(car);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("operations/{id}/transport")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> TransportToStation(Guid id, [FromBody] Guid targetDepartmentId)
        {
            await _policeService.TransportToStationAsync(id, targetDepartmentId);
            return Ok(new { message = "Rozpoczęto transport na komendę." });
        }

        [HttpPost("operations/{id}/return")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> ReturnToBase(Guid id)
        {
            await _policeService.ReturnToBaseAsync(id);
            return Ok(new { message = "Radiowóz wraca do bazy." });
        }

        [HttpPost("cars/{id}/free")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> FreePoliceCar(Guid id)
        {
            try
            {
                var car = await _context.PoliceCars.FindAsync(id);
                if (car == null) return NotFound(new { message = "Nie znaleziono radiowozu." });

                car.IsAvailable = true;
                car.Status = VehicleOperationalStatus.InBase;

                if (car.CurrentIncidentId.HasValue)
                {
                    var incidentId = car.CurrentIncidentId.Value;

                    var query = _context.PoliceOperations
                        .Where(o => o.IncidentId == incidentId && o.EndTime == null);

                    if (car.PolicemanId.HasValue)
                        query = query.Where(o => o.PolicemanId == car.PolicemanId || o.PolicemanId == null);
                    else
                        query = query.Where(o => o.PDepartmentId == car.PDepartmentId);

                    var openOps = await query.ToListAsync();
                    foreach (var op in openOps)
                    {
                        op.EndTime = DateTime.UtcNow;
                        _context.PoliceOperations.Update(op);
                    }

                    car.CurrentIncidentId = null;

                    var incident = await _context.Incidents.FindAsync(incidentId);
                    if (incident != null)
                    {
                        incident.IsPoliceActive = false;

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

                _context.PoliceCars.Update(car);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Radiowóz zwolniony." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd zwalniania radiowozu: " + ex.Message });
            }
        }
    }
}