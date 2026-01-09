using Accounting.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard([FromQuery] int branchId = 1, CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetDashboardStatsQuery(branchId), ct));
    }
}
