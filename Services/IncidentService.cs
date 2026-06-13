using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Api.Services;

public class IncidentService(AppDbContext db)
{
    private const int MaxActiveIncidentsPerTechnician = 3;
    private static readonly HashSet<IncidentStatus> ActiveStatuses =
        [IncidentStatus.Assigned, IncidentStatus.InProgress];

    // Valid forward-only transitions
    private static readonly Dictionary<IncidentStatus, IncidentStatus[]> Transitions = new()
    {
        { IncidentStatus.Registered,  [IncidentStatus.Assigned]    },
        { IncidentStatus.Assigned,    [IncidentStatus.InProgress]  },
        { IncidentStatus.InProgress,  [IncidentStatus.Resolved]    },
        { IncidentStatus.Resolved,    [IncidentStatus.Closed]      },
        { IncidentStatus.Closed,      []                            },
    };

    public async Task<(Incident? incident, string? error)> CreateAsync(CreateIncidentRequest req)
    {
        var incident = new Incident
        {
            Title        = req.Title,
            Description  = req.Description,
            SiteLocation = req.SiteLocation,
            Severity     = req.Severity,
            Type         = req.Type,
            Status       = IncidentStatus.Registered
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();
        return (incident, null);
    }

    public async Task<(Incident? incident, string? error)> AssignAsync(int incidentId, int technicianId, string? note = null)
    {
        var incident = await db.Incidents.Include(i => i.History).FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null) return (null, "Incident not found.");

        var technician = await db.Technicians.FindAsync(technicianId);
        if (technician is null || !technician.IsActive) return (null, "Technician not found or inactive.");

        // Specialization check
        var required = SpecializationRules.Required(incident.Type);
        if (technician.Specialization != required && technician.Specialization != Specialization.General)
            return (null, $"Technician specialization '{technician.Specialization}' does not match required '{required}' for this incident type.");

        // Active incidents cap
        var activeCount = await db.Incidents
            .CountAsync(i => i.TechnicianId == technicianId && ActiveStatuses.Contains(i.Status));
        if (activeCount >= MaxActiveIncidentsPerTechnician)
            return (null, $"Technician already has {MaxActiveIncidentsPerTechnician} active incidents.");

        var prevTech = incident.TechnicianId;
        var fromStatus = incident.Status;

        incident.TechnicianId = technicianId;

        // If still Registered, advance to Assigned
        if (incident.Status == IncidentStatus.Registered)
        {
            incident.Status = IncidentStatus.Assigned;
            incident.AssignedAt = DateTime.UtcNow;
        }

        AddHistory(incident, fromStatus, incident.Status, technicianId, note ?? $"Assigned to technician {technicianId}" + (prevTech.HasValue ? $" (reassigned from {prevTech})" : ""));
        await db.SaveChangesAsync();
        return (incident, null);
    }

    public async Task<(Incident? incident, string? error)> ChangeStatusAsync(int incidentId, IncidentStatus newStatus, int? technicianId, string? note = null)
    {
        var incident = await db.Incidents.Include(i => i.History).FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null) return (null, "Incident not found.");

        if (!Transitions[incident.Status].Contains(newStatus))
            return (null, $"Cannot transition from '{incident.Status}' to '{newStatus}'.");

        var fromStatus = incident.Status;
        incident.Status = newStatus;

        if (newStatus == IncidentStatus.Resolved)  incident.ResolvedAt = DateTime.UtcNow;
        if (newStatus == IncidentStatus.Closed)    incident.ClosedAt   = DateTime.UtcNow;

        AddHistory(incident, fromStatus, newStatus, technicianId, note);
        await db.SaveChangesAsync();
        return (incident, null);
    }

    public async Task<(Incident? incident, string? error)> ReleaseAsync(int incidentId, string? note = null)
    {
        var incident = await db.Incidents.Include(i => i.History).FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null) return (null, "Incident not found.");
        if (incident.Status == IncidentStatus.Closed || incident.Status == IncidentStatus.Resolved)
            return (null, "Cannot release a resolved or closed incident.");

        var prevTech = incident.TechnicianId;
        var fromStatus = incident.Status;

        incident.TechnicianId = null;
        incident.Status = IncidentStatus.Registered;

        AddHistory(incident, fromStatus, IncidentStatus.Registered, prevTech, note ?? $"Released by technician {prevTech}");
        await db.SaveChangesAsync();
        return (incident, null);
    }

    public async Task EscalateOverdueAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(-2);
        var overdueIncidents = await db.Incidents
            .Where(i => !i.IsEscalated
                && i.Status == IncidentStatus.Registered
                && (i.Severity == Severity.Critical || i.Severity == Severity.Urgent)
                && i.CreatedAt <= threshold)
            .Include(i => i.History)
            .ToListAsync();

        foreach (var incident in overdueIncidents)
        {
            incident.IsEscalated = true;
            AddHistory(incident, incident.Status, incident.Status, null, "Auto-escalated: critical/urgent incident unattended for over 2 hours.");
        }

        if (overdueIncidents.Count > 0)
            await db.SaveChangesAsync();
    }

    private static void AddHistory(Incident incident, IncidentStatus from, IncidentStatus to, int? techId, string? note)
    {
        incident.History.Add(new IncidentHistory
        {
            IncidentId   = incident.Id,
            FromStatus   = from,
            ToStatus     = to,
            TechnicianId = techId,
            Note         = note,
            ChangedAt    = DateTime.UtcNow
        });
    }
}

public record CreateIncidentRequest(
    string Title,
    string Description,
    string SiteLocation,
    Severity Severity,
    IncidentType Type
);
