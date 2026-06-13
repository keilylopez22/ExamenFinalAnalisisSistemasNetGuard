using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Tests;

/// <summary>
/// Provides a fresh isolated InMemory DbContext per test — satisfies FIRST (Fast + Isolated).
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated(); // runs seed
        return ctx;
    }

    public static Technician TechFiber(int id = 100)   => new() { Id = id, Name = "Fiber Tech",    Specialization = Specialization.FiberOptic,  IsActive = true };
    public static Technician TechMicro(int id = 101)   => new() { Id = id, Name = "Micro Tech",    Specialization = Specialization.Microwave,   IsActive = true };
    public static Technician TechGeneral(int id = 102) => new() { Id = id, Name = "General Tech",  Specialization = Specialization.General,     IsActive = true };
    public static Technician TechInactive(int id = 103)=> new() { Id = id, Name = "Inactive Tech", Specialization = Specialization.FiberOptic,  IsActive = false };

    public static Incident FiberIncident(int id = 200) => new()
    {
        Id           = id,
        Title        = "Fiber cut",
        Description  = "Cut at node A",
        SiteLocation = "Zone 1",
        Severity     = Severity.High,
        Type         = IncidentType.FiberOptic,
        Status       = IncidentStatus.Registered,
        CreatedAt    = DateTime.UtcNow
    };

    public static Incident CriticalIncident(int id = 201, DateTime? createdAt = null) => new()
    {
        Id           = id,
        Title        = "Critical outage",
        Description  = "Full outage",
        SiteLocation = "Zone 2",
        Severity     = Severity.Critical,
        Type         = IncidentType.Network,
        Status       = IncidentStatus.Registered,
        CreatedAt    = createdAt ?? DateTime.UtcNow
    };
}
