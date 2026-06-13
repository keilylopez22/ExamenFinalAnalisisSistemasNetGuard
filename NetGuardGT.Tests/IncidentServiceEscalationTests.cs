using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for IncidentService.EscalateOverdueAsync.
/// Verifies auto-escalation logic for Critical/Urgent incidents idle > 2 hours.
/// </summary>
public class IncidentServiceEscalationTests
{
    private static (IncidentService service, Api.Data.AppDbContext ctx) Setup()
    {
        var ctx = DbContextFactory.Create();
        return (new IncidentService(ctx), ctx);
    }

    private static async Task<Incident> SeedAsync(Api.Data.AppDbContext ctx,
        Severity severity, IncidentStatus status, DateTime createdAt, bool alreadyEscalated = false)
    {
        var incident = new Incident
        {
            Title = "T", Description = "D", SiteLocation = "S",
            Severity = severity, Type = IncidentType.Network,
            Status = status, CreatedAt = createdAt, IsEscalated = alreadyEscalated
        };
        ctx.Incidents.Add(incident);
        await ctx.SaveChangesAsync();
        return incident;
    }

    [Fact]
    public async Task Escalate_CriticalRegisteredOver2Hours_MarksEscalated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Critical, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddHours(-3));

        await service.EscalateOverdueAsync();

        ctx.Incidents.Find(incident.Id)!.IsEscalated.Should().BeTrue();
    }

    [Fact]
    public async Task Escalate_UrgentRegisteredOver2Hours_MarksEscalated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Urgent, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddHours(-2).AddMinutes(-1));

        await service.EscalateOverdueAsync();

        ctx.Incidents.Find(incident.Id)!.IsEscalated.Should().BeTrue();
    }

    [Fact]
    public async Task Escalate_CriticalRegisteredUnder2Hours_NotEscalated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Critical, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddMinutes(-30));

        await service.EscalateOverdueAsync();

        ctx.Incidents.Find(incident.Id)!.IsEscalated.Should().BeFalse();
    }

    [Fact]
    public async Task Escalate_HighSeverityOver2Hours_NotEscalated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.High, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddHours(-5));

        await service.EscalateOverdueAsync();

        ctx.Incidents.Find(incident.Id)!.IsEscalated.Should().BeFalse();
    }

    [Fact]
    public async Task Escalate_CriticalAlreadyAssigned_NotEscalated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Critical, IncidentStatus.Assigned,
            createdAt: DateTime.UtcNow.AddHours(-5));

        await service.EscalateOverdueAsync();

        ctx.Incidents.Find(incident.Id)!.IsEscalated.Should().BeFalse();
    }

    [Fact]
    public async Task Escalate_AlreadyEscalated_NotEscalatedAgain_AndHistoryNotDuplicated()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Critical, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddHours(-5), alreadyEscalated: true);

        await service.EscalateOverdueAsync();

        ctx.IncidentHistories.Where(h => h.IncidentId == incident.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task Escalate_AddsHistoryEntry_WithEscalationNote()
    {
        var (service, ctx) = Setup();
        var incident = await SeedAsync(ctx, Severity.Critical, IncidentStatus.Registered,
            createdAt: DateTime.UtcNow.AddHours(-3));

        await service.EscalateOverdueAsync();

        ctx.IncidentHistories
            .Where(h => h.IncidentId == incident.Id)
            .Should().ContainSingle()
            .Which.Note.Should().Contain("Auto-escalated");
    }
}
