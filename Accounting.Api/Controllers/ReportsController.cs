using Accounting.Application.Common.Abstractions;
using Accounting.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[Route("api/reports")]
[ApiController]
public class ReportsController(IMediator mediator, IExcelService excelService) : ControllerBase
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

    [HttpGet("stock-status/export")]
    public async Task<IActionResult> ExportStockStatus(CancellationToken ct)
    {
        var data = await mediator.Send(new GetStockStatusQuery(), ct);
        var fileContent = await excelService.ExportAsync(data, "StockStatus");
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"StockStatus_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet("contact/{id}/statement/export")]
    public async Task<IActionResult> ExportContactStatement(
        int id, 
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        var data = await mediator.Send(new GetContactStatementQuery(id, dateFrom, dateTo), ct);
        var fileContent = await excelService.ExportAsync(data.Items, "Statement");
        
        var safeName = string.Join("_", data.ContactName.Split(Path.GetInvalidFileNameChars()));
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ekstre_{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet("profit-loss")]
    public async Task<ActionResult<ProfitLossDto>> GetProfitLoss(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetProfitLossQuery(dateFrom, dateTo), ct));
    }
}
