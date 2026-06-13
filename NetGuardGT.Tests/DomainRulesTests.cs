using FluentAssertions;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Tests for SpecializationRules and Incident.SlaHours computed property.
/// Pure unit tests — no DB, no async, no infrastructure (maximally Fast).
/// </summary>
public class DomainRulesTests
{
    // ── Specialization mapping ───────────────────────────────────────────────

    [Theory]
    [InlineData(IncidentType.FiberOptic,  Specialization.FiberOptic)]
    [InlineData(IncidentType.Microwave,   Specialization.Microwave)]
    [InlineData(IncidentType.Electrical,  Specialization.Electrical)]
    [InlineData(IncidentType.Network,     Specialization.Network)]
    [InlineData(IncidentType.Other,       Specialization.General)]
    public void SpecializationRules_ReturnsCorrectSpecialization(IncidentType type, Specialization expected)
    {
        SpecializationRules.Required(type).Should().Be(expected);
    }

    // ── SLA hours ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Severity.Critical, 2)]
    [InlineData(Severity.Urgent,   4)]
    [InlineData(Severity.High,     8)]
    [InlineData(Severity.Medium,   24)]
    [InlineData(Severity.Low,      48)]
    public void Incident_SlaHours_MatchesSeverity(Severity severity, int expectedHours)
    {
        var incident = new Incident { Severity = severity };
        incident.SlaHours.Should().Be(expectedHours);
    }

    // ── default values ───────────────────────────────────────────────────────

    [Fact]
    public void Incident_DefaultStatus_IsRegistered()
    {
        var incident = new Incident();
        incident.Status.Should().Be(IncidentStatus.Registered);
    }

    [Fact]
    public void Incident_DefaultIsEscalated_IsFalse()
    {
        var incident = new Incident();
        incident.IsEscalated.Should().BeFalse();
    }

    [Fact]
    public void Technician_DefaultIsActive_IsTrue()
    {
        var tech = new Technician();
        tech.IsActive.Should().BeTrue();
    }
}
