using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetMarketplaceAPI.Data;
using PetMarketplaceAPI.Models;
using System.Security.Claims;

namespace PetMarketplaceAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VaccinationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VaccinationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Vaccinations/pet/5 - Get vaccination history for a pet
        [HttpGet("pet/{petId}")]
        public async Task<ActionResult> GetPetVaccinations(int petId)
        {
            var vaccinations = await _context.VaccinationRecords
                .Where(v => v.PetId == petId)
                .OrderByDescending(v => v.VaccinationDate)
                .ToListAsync();

            return Ok(vaccinations);
        }

        // POST: api/Vaccinations - Add vaccination record
        [HttpPost]
        public async Task<ActionResult> AddVaccination([FromBody] VaccinationRecord vaccination)
        {
            vaccination.CreatedAt = DateTime.UtcNow;

            _context.VaccinationRecords.Add(vaccination);

            // Update pet's vaccination info
            var pet = await _context.Pets.FindAsync(vaccination.PetId);
            if (pet != null)
            {
                pet.IsVaccinated = true;
                pet.LastVaccinationDate = vaccination.VaccinationDate;
                pet.NextVaccinationDate = vaccination.NextDueDate;
                pet.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Vaccination added successfully" });
        }

        // PUT: api/Vaccinations/5 - Update vaccination record
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVaccination(int id, [FromBody] VaccinationRecord updatedVaccination)
        {
            var vaccination = await _context.VaccinationRecords.FindAsync(id);

            if (vaccination == null) return NotFound();

            vaccination.VaccineName = updatedVaccination.VaccineName;
            vaccination.VaccinationDate = updatedVaccination.VaccinationDate;
            vaccination.NextDueDate = updatedVaccination.NextDueDate;
            vaccination.VeterinarianName = updatedVaccination.VeterinarianName;
            vaccination.ClinicName = updatedVaccination.ClinicName;
            vaccination.Notes = updatedVaccination.Notes;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Vaccination updated" });
        }

        // DELETE: api/Vaccinations/5 - Delete vaccination record
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVaccination(int id)
        {
            var vaccination = await _context.VaccinationRecords.FindAsync(id);

            if (vaccination == null) return NotFound();

            _context.VaccinationRecords.Remove(vaccination);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vaccination deleted" });
        }
    }
}