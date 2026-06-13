namespace NetGuardGT.Api.Models;

public class IncidentHistory
{
    public int Id { get; set; }
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public IncidentStatus FromStatus { get; set; }
    public IncidentStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public int? TechnicianId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
