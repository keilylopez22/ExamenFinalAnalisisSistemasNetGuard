using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Api.Services;

public class ReportService(AppDbContext db)
{
    public async Task<IncidentReportDto> GetIncidentReportAsync(DateTime? from, DateTime? to)
    {
        var query = db.Incidents
            .Include(i => i.Technician)
            .AsQueryable();

        if (from.HasValue) query = query.Where(i => i.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(i => i.CreatedAt <= to.Value);

        var incidents = await query.ToListAsync();

        var slaBreaches = incidents.Where(i =>
            i.ResolvedAt.HasValue &&
            (i.ResolvedAt.Value - i.CreatedAt).TotalHours > i.SlaHours).ToList();

        return new IncidentReportDto
        {
            Total             = incidents.Count,
            ByStatus          = incidents.GroupBy(i => i.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            BySeverity        = incidents.GroupBy(i => i.Severity).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            Escalated         = incidents.Count(i => i.IsEscalated),
            SlaBreaches       = slaBreaches.Count,
            SlaComplianceRate = incidents.Count == 0 ? 100 :
                                Math.Round((1.0 - (double)slaBreaches.Count / incidents.Count) * 100, 2),
            ByTechnician      = incidents
                .Where(i => i.Technician != null)
                .GroupBy(i => i.Technician!.Name)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<TechnicianWorkloadDto> GetTechnicianWorkloadAsync()
    {
        var technicians = await db.Technicians
            .Include(t => t.Incidents)
            .Where(t => t.IsActive)
            .ToListAsync();

        var activeStatuses = new[] { IncidentStatus.Assigned, IncidentStatus.InProgress };

        return new TechnicianWorkloadDto
        {
            Workloads = technicians.Select(t => new TechnicianWorkload
            {
                TechnicianId   = t.Id,
                Name           = t.Name,
                Specialization = t.Specialization.ToString(),
                ActiveIncidents = t.Incidents.Count(i => activeStatuses.Contains(i.Status)),
                TotalAssigned   = t.Incidents.Count
            }).OrderByDescending(w => w.ActiveIncidents).ToList()
        };
    }

    public async Task<SlaReportDto> GetSlaReportAsync()
    {
        var resolved = await db.Incidents
            .Where(i => i.ResolvedAt.HasValue)
            .ToListAsync();

        var results = resolved.Select(i => new SlaIncidentDto
        {
            IncidentId    = i.Id,
            Title         = i.Title,
            Severity      = i.Severity.ToString(),
            SlaHours      = i.SlaHours,
            ActualHours   = Math.Round((i.ResolvedAt!.Value - i.CreatedAt).TotalHours, 2),
            SlaBreached   = (i.ResolvedAt.Value - i.CreatedAt).TotalHours > i.SlaHours
        }).ToList();

        return new SlaReportDto
        {
            Total         = results.Count,
            Breached      = results.Count(r => r.SlaBreached),
            ComplianceRate = results.Count == 0 ? 100 :
                             Math.Round((1.0 - (double)results.Count(r => r.SlaBreached) / results.Count) * 100, 2),
            Details       = results
        };
    }
}

public record IncidentReportDto
{
    public int Total { get; init; }
    public Dictionary<string, int> ByStatus { get; init; } = [];
    public Dictionary<string, int> BySeverity { get; init; } = [];
    public int Escalated { get; init; }
    public int SlaBreaches { get; init; }
    public double SlaComplianceRate { get; init; }
    public Dictionary<string, int> ByTechnician { get; init; } = [];
}

public record TechnicianWorkloadDto
{
    public List<TechnicianWorkload> Workloads { get; init; } = [];
}

public record TechnicianWorkload
{
    public int TechnicianId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Specialization { get; init; } = string.Empty;
    public int ActiveIncidents { get; init; }
    public int TotalAssigned { get; init; }
}

public record SlaReportDto
{
    public int Total { get; init; }
    public int Breached { get; init; }
    public double ComplianceRate { get; init; }
    public List<SlaIncidentDto> Details { get; init; } = [];
}

public record SlaIncidentDto
{
    public int IncidentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int SlaHours { get; init; }
    public double ActualHours { get; init; }
    public bool SlaBreached { get; init; }
}
