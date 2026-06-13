using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for IncidentService.AssignAsync — covers specialization rule,
/// max-active-incidents cap, reassignment, and inactive technician guard.
/// </summary>
public class IncidentServiceAssignTests
{
    private static (IncidentService service, Api.Data.AppDbContext ctx) Setup()
    {
        var ctx = DbContextFactory.Create();
        return (new IncidentService(ctx), ctx);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task<Incident> SeedIncidentAsync(Api.Data.AppDbContext ctx,
        IncidentType type = IncidentType.FiberOptic,
        IncidentStatus status = IncidentStatus.Registered,
        int? techId = null)
    {
        var incident = new Incident
        {
            Title = "T", Description = "D", SiteLocation = "S",
            Severity = Severity.High, Type = type,
            Status = status, TechnicianId = techId, CreatedAt = DateTime.UtcNow
        };
        ctx.Incidents.Add(incident);
        await ctx.SaveChangesAsync();
        return incident;
    }

    private static async Task<Technician> SeedTechAsync(Api.Data.AppDbContext ctx, Technician tech)
    {
        ctx.Technicians.Add(tech);
        await ctx.SaveChangesAsync();
        return tech;
    }

    // ── happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_MatchingSpecialization_AdvancesToAssigned()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechFiber());
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(incident.Id, tech.Id);

        error.Should().BeNull();
        result!.Status.Should().Be(IncidentStatus.Assigned);
        result.TechnicianId.Should().Be(tech.Id);
    }

    [Fact]
    public async Task Assign_GeneralTechnician_CanHandleAnyIncidentType()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechGeneral());
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(incident.Id, tech.Id);

        error.Should().BeNull();
        result!.Status.Should().Be(IncidentStatus.Assigned);
    }

    [Fact]
    public async Task Assign_AddsHistoryEntry()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechFiber());
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        await service.AssignAsync(incident.Id, tech.Id);

        ctx.IncidentHistories.Where(h => h.IncidentId == incident.Id).Should().HaveCount(1);
    }

    [Fact]
    public async Task Assign_Reassignment_UpdatesTechnicianId()
    {
        var (service, ctx) = Setup();
        var tech1 = await SeedTechAsync(ctx, DbContextFactory.TechFiber(100));
        var tech2 = await SeedTechAsync(ctx, DbContextFactory.TechFiber(101));
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic, IncidentStatus.Assigned, tech1.Id);

        var (result, error) = await service.AssignAsync(incident.Id, tech2.Id);

        error.Should().BeNull();
        result!.TechnicianId.Should().Be(tech2.Id);
    }

    // ── business rule violations ─────────────────────────────────────────────

    [Fact]
    public async Task Assign_WrongSpecialization_ReturnsError()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechMicro()); // Microwave
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(incident.Id, tech.Id);

        result.Should().BeNull();
        error.Should().Contain("does not match required");
    }

    [Fact]
    public async Task Assign_InactiveTechnician_ReturnsError()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechInactive());
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(incident.Id, tech.Id);

        result.Should().BeNull();
        error.Should().Contain("inactive");
    }

    [Fact]
    public async Task Assign_NonExistentTechnician_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(incident.Id, 9999);

        result.Should().BeNull();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task Assign_NonExistentIncident_ReturnsError()
    {
        var (service, ctx) = Setup();
        await SeedTechAsync(ctx, DbContextFactory.TechFiber());

        var (result, error) = await service.AssignAsync(9999, 100);

        result.Should().BeNull();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task Assign_ExceedsMaxActiveIncidents_ReturnsError()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechFiber());

        // Add 3 active incidents already assigned
        for (int i = 0; i < 3; i++)
            await SeedIncidentAsync(ctx, IncidentType.FiberOptic, IncidentStatus.Assigned, tech.Id);

        var newIncident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(newIncident.Id, tech.Id);

        result.Should().BeNull();
        error.Should().Contain("3 active incidents");
    }

    [Fact]
    public async Task Assign_TechnicianWithTwoActive_CanStillAcceptOne_More()
    {
        var (service, ctx) = Setup();
        var tech = await SeedTechAsync(ctx, DbContextFactory.TechFiber());

        for (int i = 0; i < 2; i++)
            await SeedIncidentAsync(ctx, IncidentType.FiberOptic, IncidentStatus.Assigned, tech.Id);

        var newIncident = await SeedIncidentAsync(ctx, IncidentType.FiberOptic);

        var (result, error) = await service.AssignAsync(newIncident.Id, tech.Id);

        error.Should().BeNull();
        result.Should().NotBeNull();
    }
}
