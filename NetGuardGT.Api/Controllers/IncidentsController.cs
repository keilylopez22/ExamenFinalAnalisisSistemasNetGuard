using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController(AppDbContext db, IncidentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] IncidentStatus? status,
        [FromQuery] Severity? severity,
        [FromQuery] bool? escalated)
    {
        var query = db.Incidents.Include(i => i.Technician).AsQueryable();
        if (status   != null) query = query.Where(i => i.Status      == status);
        if (severity != null) query = query.Where(i => i.Severity    == severity);
        if (escalated!= null) query = query.Where(i => i.IsEscalated == escalated);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var incident = await db.Incidents
            .Include(i => i.Technician)
            .Include(i => i.History)
            .FirstOrDefaultAsync(i => i.Id == id);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest req)
    {
        var (incident, error) = await service.CreateAsync(req);
        if (error != null) return BadRequest(new { error });
        return CreatedAtAction(nameof(GetById), new { id = incident!.Id }, incident);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest req)
    {
        var (incident, error) = await service.AssignAsync(id, req.TechnicianId, req.Note);
        if (error != null) return BadRequest(new { error });
        return Ok(incident);
    }

    [HttpPost("{id}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest req)
    {
        var (incident, error) = await service.ChangeStatusAsync(id, req.NewStatus, req.TechnicianId, req.Note);
        if (error != null) return BadRequest(new { error });
        return Ok(incident);
    }

    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(int id, [FromBody] ReleaseRequest req)
    {
        var (incident, error) = await service.ReleaseAsync(id, req.Note);
        if (error != null) return BadRequest(new { error });
        return Ok(incident);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await db.IncidentHistories
            .Where(h => h.IncidentId == id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
        return Ok(history);
    }
}

public record AssignRequest(int TechnicianId, string? Note);
public record ChangeStatusRequest(IncidentStatus NewStatus, int? TechnicianId, string? Note);
public record ReleaseRequest(string? Note);
