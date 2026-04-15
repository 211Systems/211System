using _211system.Data;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            await _policeService.DeletePolicemanAsync(id);
            return Ok(new { message = "Zwolniono policjanta." });
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

        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
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
            var cars = await _context.PoliceCars.ToListAsync();
            return Ok(cars);
        }

        [HttpDelete("cars/{id}")]
        [Authorize(Roles = "Admin, Komendant, Inspektor")]
        public async Task<IActionResult> DeleteCar(Guid id)
        {
            var car = await _context.PoliceCars.FindAsync(id);
            if (car == null) return NotFound();
            _context.PoliceCars.Remove(car);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Usunięto radiowóz." });
        }
        [HttpGet("operations")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetOperations()
        {
            var operations = await _context.PoliceOperations.ToListAsync();
            return Ok(operations);
        }

        [HttpPost("operations/start")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid policemanId, [FromQuery] Guid incidentId) 
        {
            var operation = new PoliceOperation
            {
                PolicemanId = policemanId,
                IncidentId = incidentId,
                StartTime = DateTime.UtcNow
            };

            _context.PoliceOperations.Add(operation);
            await _context.SaveChangesAsync();
            return Ok(operation);
        }

        [HttpPut("operations/{id}/end")]
        [Authorize(Roles = "Admin, Komendant, Inspektor, Policjant")]
        public async Task<IActionResult> EndOperation(Guid id)
        {
            var operation = await _context.PoliceOperations.FindAsync(id);
            if (operation == null) return NotFound();

            operation.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Interwencja zakończona." });
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
        [Authorize(Roles = "Admin, Inspektor")]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] CreatePDepartmentDto dto)
        {
            var dept = await _context.PoliceDepartments.FindAsync(id);
            if (dept == null) return NotFound();

            dept.Name = dto.Name;
            dept.Address = dto.Address;
            dept.District = dto.District;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaktualizowano placówkę." });
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
    }
}