using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for IncidentService.ChangeStatusAsync and ReleaseAsync.
/// Validates the forward-only state machine and release flow.
/// </summary>
public class IncidentServiceStatusTests
{
    private static (IncidentService service, Api.Data.AppDbContext ctx) Setup()
    {
        var ctx = DbContextFactory.Create();
        return (new IncidentService(ctx), ctx);
    }

    private static async Task<Incident> SeedAsync(Api.Data.AppDbContext ctx,
        IncidentStatus status, int? techId = null)
    {
        var incident = new Incident
        {
            Title = "T", Description = "D", SiteLocation = "S",
            Severity = Severity.Medium, Type = IncidentType.Network,
            Status = status, TechnicianId = techId, CreatedAt = DateTime.UtcNow
        };
        ctx.Incidents.Add(incident);
        await ctx.SaveChangesAsync();
        return incident;
    }

    // ── valid forward transitions ────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_AssignedToInProgress_Succeeds()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Assigned);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.InProgress, null);

        error.Should().BeNull();
        result!.Status.Should().Be(IncidentStatus.InProgress);
    }

    [Fact]
    public async Task ChangeStatus_InProgressToResolved_SetsResolvedAt()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.InProgress);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.Resolved, null);

        error.Should().BeNull();
        result!.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_ResolvedToClosed_SetsClosedAt()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Resolved);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.Closed, null);

        error.Should().BeNull();
        result!.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_AddsHistoryOnTransition()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Assigned);

        await service.ChangeStatusAsync(incident.Id, IncidentStatus.InProgress, null);

        ctx.IncidentHistories.Where(h => h.IncidentId == incident.Id).Should().HaveCount(1);
    }

    // ── invalid / backward transitions ──────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_RegisteredToInProgress_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Registered);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.InProgress, null);

        result.Should().BeNull();
        error.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task ChangeStatus_InProgressToAssigned_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.InProgress);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.Assigned, null);

        result.Should().BeNull();
        error.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task ChangeStatus_ClosedToAny_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Closed);

        var (result, error) = await service.ChangeStatusAsync(incident.Id, IncidentStatus.Resolved, null);

        result.Should().BeNull();
        error.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task ChangeStatus_NonExistentIncident_ReturnsError()
    {
        var (service, _) = Setup();

        var (result, error) = await service.ChangeStatusAsync(9999, IncidentStatus.InProgress, null);

        result.Should().BeNull();
        error.Should().Contain("not found");
    }

    // ── release ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Release_AssignedIncident_ResetsToRegisteredAndClearsTechnician()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Assigned, techId: 1);

        var (result, error) = await service.ReleaseAsync(incident.Id);

        error.Should().BeNull();
        result!.Status.Should().Be(IncidentStatus.Registered);
        result.TechnicianId.Should().BeNull();
    }

    [Fact]
    public async Task Release_ResolvedIncident_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Resolved);

        var (result, error) = await service.ReleaseAsync(incident.Id);

        result.Should().BeNull();
        error.Should().Contain("Cannot release");
    }

    [Fact]
    public async Task Release_ClosedIncident_ReturnsError()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Closed);

        var (result, error) = await service.ReleaseAsync(incident.Id);

        result.Should().BeNull();
        error.Should().Contain("Cannot release");
    }

    [Fact]
    public async Task Release_AddsHistoryEntry()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, IncidentStatus.Assigned, techId: 1);

        await service.ReleaseAsync(incident.Id);

        ctx.IncidentHistories.Where(h => h.IncidentId == incident.Id).Should().HaveCount(1);
    }
}
