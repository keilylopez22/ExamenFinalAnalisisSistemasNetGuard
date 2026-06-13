using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for IncidentService.CreateAsync
/// FIRST: Fast, Isolated (fresh DB per test), Repeatable, Self-validating, Timely
/// </summary>
public class IncidentServiceCreateTests
{
    private static IncidentService BuildService(out Api.Data.AppDbContext ctx)
    {
        ctx = DbContextFactory.Create();
        return new IncidentService(ctx);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsIncidentWithRegisteredStatus()
    {
        var service = BuildService(out _);
        var req = new CreateIncidentRequest("Fiber cut", "Node A down", "Zone 1", Severity.High, IncidentType.FiberOptic);

        var (incident, error) = await service.CreateAsync(req);

        error.Should().BeNull();
        incident.Should().NotBeNull();
        incident!.Status.Should().Be(IncidentStatus.Registered);
    }

    [Fact]
    public async Task Create_ValidRequest_PersistsToDatabase()
    {
        var service = BuildService(out var ctx);
        var req = new CreateIncidentRequest("Node fail", "Node B", "Zone 2", Severity.Critical, IncidentType.Network);

        var (incident, _) = await service.CreateAsync(req);

        ctx.Incidents.Find(incident!.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_SetsCorrectSlaHours_ForCriticalSeverity()
    {
        var service = BuildService(out _);
        var req = new CreateIncidentRequest("Critical", "Full outage", "Zone 3", Severity.Critical, IncidentType.Network);

        var (incident, _) = await service.CreateAsync(req);

        incident!.SlaHours.Should().Be(2);
    }

    [Fact]
    public async Task Create_SetsCorrectSlaHours_ForLowSeverity()
    {
        var service = BuildService(out _);
        var req = new CreateIncidentRequest("Low", "Minor issue", "Zone 4", Severity.Low, IncidentType.Other);

        var (incident, _) = await service.CreateAsync(req);

        incident!.SlaHours.Should().Be(48);
    }

    [Fact]
    public async Task Create_IsEscalated_DefaultsFalse()
    {
        var service = BuildService(out _);
        var req = new CreateIncidentRequest("Test", "Desc", "Zone 5", Severity.Medium, IncidentType.Electrical);

        var (incident, _) = await service.CreateAsync(req);

        incident!.IsEscalated.Should().BeFalse();
    }
}
