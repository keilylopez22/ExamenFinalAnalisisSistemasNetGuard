using Microsoft.AspNetCore.Mvc;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ReportService reportService) : ControllerBase
{
    [HttpGet("incidents")]
    public async Task<IActionResult> IncidentReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await reportService.GetIncidentReportAsync(from, to));

    [HttpGet("workload")]
    public async Task<IActionResult> WorkloadReport() =>
        Ok(await reportService.GetTechnicianWorkloadAsync());

    [HttpGet("sla")]
    public async Task<IActionResult> SlaReport() =>
        Ok(await reportService.GetSlaReportAsync());
}
