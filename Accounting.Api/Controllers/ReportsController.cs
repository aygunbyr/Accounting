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

    [HttpGet("contact/{contactId}/statement")]
    public async Task<ActionResult<ContactStatementDto>> GetContactStatement(
        int contactId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetContactStatementQuery(contactId, dateFrom, dateTo), ct));
    }

    [HttpGet("stock-status")]
    public async Task<ActionResult<List<StockStatusDto>>> GetStockStatus(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetStockStatusQuery(), ct));
    }
}
