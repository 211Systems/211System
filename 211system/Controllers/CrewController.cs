using _211system.Data;
using _211system.DTOs;
using _211system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrewController : ControllerBase
    {
        private readonly _211DbContext _context;

        private const int MaxAdditionalCrew = 4;

        private static readonly string[] AllowedTypes = { "ambulance", "police", "fire", "air" };

        public CrewController(_211DbContext context)
        {
            _context = context;
        }

        [HttpGet("{vehicleType}/{vehicleId}")]
        [Authorize]
        public async Task<IActionResult> GetCrew(string vehicleType, Guid vehicleId)
        {
            vehicleType = (vehicleType ?? "").ToLowerInvariant();
            if (!AllowedTypes.Contains(vehicleType))
                return BadRequest(new { message = "Nieprawidłowy typ pojazdu." });

            var crew = await _context.VehicleCrews
                .Where(c => c.VehicleType == vehicleType && c.VehicleId == vehicleId)
                .Select(c => new CrewMemberDto { MemberId = c.MemberId, MemberName = c.MemberName })
                .ToListAsync();

            return Ok(crew);
        }

        [HttpPut("{vehicleType}/{vehicleId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan")]
        public async Task<IActionResult> SetCrew(string vehicleType, Guid vehicleId, [FromBody] SetCrewDto dto)
        {
            vehicleType = (vehicleType ?? "").ToLowerInvariant();
            if (!AllowedTypes.Contains(vehicleType))
                return BadRequest(new { message = "Nieprawidłowy typ pojazdu." });

            var incoming = (dto?.Crew ?? new List<CrewMemberDto>())
                .Where(c => c.MemberId != Guid.Empty)
                .GroupBy(c => c.MemberId)
                .Select(g => g.First())
                .ToList();

            if (incoming.Count > MaxAdditionalCrew)
                return BadRequest(new { message = $"Maksymalnie {MaxAdditionalCrew} dodatkowych członków załogi (łącznie z dowódcą do 5 osób)." });

            var existing = await _context.VehicleCrews
                .Where(c => c.VehicleType == vehicleType && c.VehicleId == vehicleId)
                .ToListAsync();
            _context.VehicleCrews.RemoveRange(existing);

            foreach (var m in incoming)
            {
                _context.VehicleCrews.Add(new VehicleCrew
                {
                    VehicleId = vehicleId,
                    VehicleType = vehicleType,
                    MemberId = m.MemberId,
                    MemberName = m.MemberName
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Obsada pojazdu zaktualizowana.", count = incoming.Count });
        }
    }
}
