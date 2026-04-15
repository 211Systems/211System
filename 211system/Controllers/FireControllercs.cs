using _211system.Data;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using FireDepartment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
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

        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        [HttpPut("firetrucks/{truckId}/assign/{incidentId}")]
        public async Task<IActionResult> AssignFireTruckToIncident(Guid truckId, Guid incidentId)
        {
            try
            {
                await _fireService.AssignFireTruckToIncidentAsync(truckId, incidentId);
                return Ok(new { message = "Wóz został zadysponowany do zgłoszenia." });
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
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid firemanId, [FromQuery] Guid reportId)
        {
            var fireman = await _context.Firemen.FindAsync(firemanId);
            if (fireman == null) return BadRequest("Nie znaleziono strażaka w systemie.");

            var operation = new FireDepartmentOperation
            {
                FiremanId = firemanId,
                IncidentId = reportId,
                FDepartmentId = fireman.FDepartmentId,
                StartTime = DateTime.UtcNow
            };

            _context.FireOperations.Add(operation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Wyruszono na akcję!" });
        }

        [HttpPut("operations/{id}/end")]
        [Authorize(Roles = "Admin, Naczelnik, Kapitan, Strazak")]
        public async Task<IActionResult> EndOperation(Guid id)
        {
            var operation = await _context.FireOperations.FindAsync(id);
            if (operation == null) return NotFound();

            operation.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Akcja ratownicza zakończona." });
        }

        [HttpGet("incidents/{id}")]
        [Authorize]
        public async Task<IActionResult> GetIncidentDetails(Guid id)
        {
            var incident = await _context.Incidents.FindAsync(id);
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

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano placówkę." });
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
    }
}