using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Api.Controllers;

[ApiController]
[Route("api/technicians")]
public class TechniciansController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Technicians.Where(t => t.IsActive).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tech = await db.Technicians
            .Include(t => t.Incidents)
            .FirstOrDefaultAsync(t => t.Id == id);
        return tech is null ? NotFound() : Ok(tech);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TechnicianRequest req)
    {
        var tech = new Technician { Name = req.Name, Specialization = req.Specialization };
        db.Technicians.Add(tech);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = tech.Id }, tech);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TechnicianRequest req)
    {
        var tech = await db.Technicians.FindAsync(id);
        if (tech is null) return NotFound();
        tech.Name = req.Name;
        tech.Specialization = req.Specialization;
        await db.SaveChangesAsync();
        return Ok(tech);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var tech = await db.Technicians.FindAsync(id);
        if (tech is null) return NotFound();
        tech.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record TechnicianRequest(string Name, Specialization Specialization);
