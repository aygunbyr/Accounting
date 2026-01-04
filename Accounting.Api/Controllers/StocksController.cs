using Accounting.Application.Common.Models;
using Accounting.Application.Stocks.Queries.Dto;
using Accounting.Application.Stocks.Queries.GetById;
using Accounting.Application.Stocks.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StocksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StockListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> List([FromQuery] ListStocksQuery q, CancellationToken ct)
    {
        var res = await _mediator.Send(q, ct);
        return Ok(res);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetStockByIdQuery(id), ct);
        return Ok(res);
    }
}
