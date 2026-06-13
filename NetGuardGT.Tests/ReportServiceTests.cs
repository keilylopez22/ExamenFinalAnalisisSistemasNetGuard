using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for ReportService — incident report, workload, and SLA compliance.
/// </summary>
public class ReportServiceTests
{
    private static (ReportService service, Api.Data.AppDbContext ctx) Setup()
    {
        var ctx = DbContextFactory.Create();
        return (new ReportService(ctx), ctx);
    }

    private static Incident MakeIncident(Severity severity, IncidentStatus status,
        DateTime createdAt, DateTime? resolvedAt = null, bool escalated = false, Technician? tech = null) => new()
    {
        Title = "T", Description = "D", SiteLocation = "S",
        Severity = severity, Type = IncidentType.Network,
        Status = status, CreatedAt = createdAt, ResolvedAt = resolvedAt,
        IsEscalated = escalated, Technician = tech
    };

    // ── incident report ──────────────────────────────────────────────────────

    [Fact]
    public async Task IncidentReport_EmptyDb_ReturnsTotalZeroAnd100PercentCompliance()
    {
        var (service, ctx) = Setup();
        // Remove seeded technicians' incidents (none exist) — DB is empty of incidents
        var report = await service.GetIncidentReportAsync(null, null);

        report.Total.Should().Be(0);
        report.SlaComplianceRate.Should().Be(100);
    }

    [Fact]
    public async Task IncidentReport_CountsEscalatedCorrectly()
    {
        var (service, ctx) = Setup();
        ctx.Incidents.AddRange(
            MakeIncident(Severity.Critical, IncidentStatus.Registered, DateTime.UtcNow, escalated: true),
            MakeIncident(Severity.High,     IncidentStatus.Registered, DateTime.UtcNow, escalated: false)
        );
        await ctx.SaveChangesAsync();

        var report = await service.GetIncidentReportAsync(null, null);

        report.Escalated.Should().Be(1);
    }

    [Fact]
    public async Task IncidentReport_DateFilter_OnlyReturnsIncidentsInRange()
    {
        var (service, ctx) = Setup();
        var baseDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        ctx.Incidents.AddRange(
            MakeIncident(Severity.Low, IncidentStatus.Registered, baseDate.AddDays(-10)),
            MakeIncident(Severity.Low, IncidentStatus.Registered, baseDate),
            MakeIncident(Severity.Low, IncidentStatus.Registered, baseDate.AddDays(10))
        );
        await ctx.SaveChangesAsync();

        var report = await service.GetIncidentReportAsync(
            from: baseDate.AddDays(-1),
            to:   baseDate.AddDays(1));

        report.Total.Should().Be(1);
    }

    [Fact]
    public async Task IncidentReport_SlaBreachDetected_WhenResolutionExceedsSlaHours()
    {
        var (service, ctx) = Setup();
        var created = DateTime.UtcNow.AddHours(-10);
        // Critical SLA = 2h, resolved after 5h → breach
        ctx.Incidents.Add(MakeIncident(Severity.Critical, IncidentStatus.Resolved,
            created, resolvedAt: created.AddHours(5)));
        await ctx.SaveChangesAsync();

        var report = await service.GetIncidentReportAsync(null, null);

        report.SlaBreaches.Should().Be(1);
        report.SlaComplianceRate.Should().BeLessThan(100);
    }

    [Fact]
    public async Task IncidentReport_NoSlaBreachWhenResolvedWithinSla()
    {
        var (service, ctx) = Setup();
        var created = DateTime.UtcNow.AddHours(-5);
        // High SLA = 8h, resolved after 3h → no breach
        ctx.Incidents.Add(MakeIncident(Severity.High, IncidentStatus.Resolved,
            created, resolvedAt: created.AddHours(3)));
        await ctx.SaveChangesAsync();

        var report = await service.GetIncidentReportAsync(null, null);

        report.SlaBreaches.Should().Be(0);
        report.SlaComplianceRate.Should().Be(100);
    }

    // ── workload report ──────────────────────────────────────────────────────

    [Fact]
    public async Task WorkloadReport_OnlyIncludesActiveTechnicians()
    {
        var (service, ctx) = Setup();
        ctx.Technicians.Add(DbContextFactory.TechInactive(200));
        await ctx.SaveChangesAsync();

        var report = await service.GetTechnicianWorkloadAsync();

        report.Workloads.Should().NotContain(w => w.Name == "Inactive Tech");
    }

    [Fact]
    public async Task WorkloadReport_CountsActiveIncidentsCorrectly()
    {
        var (service, ctx) = Setup();
        var tech = DbContextFactory.TechFiber(200);
        ctx.Technicians.Add(tech);
        ctx.Incidents.AddRange(
            new Incident { Title="T", Description="D", SiteLocation="S", Severity=Severity.Low,
                Type=IncidentType.FiberOptic, Status=IncidentStatus.Assigned,
                TechnicianId=200, CreatedAt=DateTime.UtcNow },
            new Incident { Title="T2", Description="D", SiteLocation="S", Severity=Severity.Low,
                Type=IncidentType.FiberOptic, Status=IncidentStatus.Closed,
                TechnicianId=200, CreatedAt=DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var report = await service.GetTechnicianWorkloadAsync();
        var workload = report.Workloads.FirstOrDefault(w => w.TechnicianId == 200);

        workload.Should().NotBeNull();
        workload!.ActiveIncidents.Should().Be(1);
        workload.TotalAssigned.Should().Be(2);
    }

    [Fact]
    public async Task WorkloadReport_OrderedByActiveIncidentsDescending()
    {
        var (service, ctx) = Setup();
        ctx.Technicians.AddRange(DbContextFactory.TechFiber(200), DbContextFactory.TechFiber(201));
        // Tech 200 has 2 active, Tech 201 has 0
        ctx.Incidents.AddRange(
            new Incident { Title="T1", Description="D", SiteLocation="S", Severity=Severity.Low,
                Type=IncidentType.FiberOptic, Status=IncidentStatus.Assigned,
                TechnicianId=200, CreatedAt=DateTime.UtcNow },
            new Incident { Title="T2", Description="D", SiteLocation="S", Severity=Severity.Low,
                Type=IncidentType.FiberOptic, Status=IncidentStatus.InProgress,
                TechnicianId=200, CreatedAt=DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var report = await service.GetTechnicianWorkloadAsync();
        var ids = report.Workloads.Select(w => w.TechnicianId).ToList();

        ids.IndexOf(200).Should().BeLessThan(ids.IndexOf(201));
    }

    // ── SLA report ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SlaReport_EmptyDb_Returns100PercentCompliance()
    {
        var (service, _) = Setup();

        var report = await service.GetSlaReportAsync();

        report.ComplianceRate.Should().Be(100);
        report.Total.Should().Be(0);
    }

    [Fact]
    public async Task SlaReport_MixedBreachAndCompliant_CalculatesRateCorrectly()
    {
        var (service, ctx) = Setup();
        var now = DateTime.UtcNow;
        ctx.Incidents.AddRange(
            // Breach: Critical resolved after 5h (SLA = 2h)
            MakeIncident(Severity.Critical, IncidentStatus.Resolved, now.AddHours(-6), now.AddHours(-1)),
            // Compliant: High resolved after 3h (SLA = 8h)
            MakeIncident(Severity.High, IncidentStatus.Resolved, now.AddHours(-5), now.AddHours(-2))
        );
        await ctx.SaveChangesAsync();

        var report = await service.GetSlaReportAsync();

        report.Total.Should().Be(2);
        report.Breached.Should().Be(1);
        report.ComplianceRate.Should().Be(50);
    }

    [Fact]
    public async Task SlaReport_UnresolvedIncidents_NotIncluded()
    {
        var (service, ctx) = Setup();
        ctx.Incidents.Add(MakeIncident(Severity.Critical, IncidentStatus.Registered, DateTime.UtcNow));
        await ctx.SaveChangesAsync();

        var report = await service.GetSlaReportAsync();

        report.Total.Should().Be(0);
    }
}
