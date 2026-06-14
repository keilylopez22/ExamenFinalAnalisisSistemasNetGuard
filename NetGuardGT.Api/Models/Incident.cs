namespace NetGuardGT.Api.Models;

public class Incident
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public IncidentType Type { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Registered;
    public bool IsEscalated { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public ICollection<IncidentHistory> History { get; set; } = new List<IncidentHistory>();

    // SLA resolution time in hours based on severity
    public int SlaHours => Severity switch
    {
        Severity.Critical => 2,
        Severity.Urgent   => 4,
        Severity.High     => 8,
        Severity.Medium   => 24,
        Severity.Low      => 48,
        _                 => 48
    };
}
