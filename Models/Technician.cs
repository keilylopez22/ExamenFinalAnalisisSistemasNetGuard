namespace NetGuardGT.Api.Models;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Specialization Specialization { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
