using Accounting.Application.Expenses.Commands.AddLine;
using Accounting.Application.Expenses.Commands.CreateList;
using Accounting.Application.Expenses.Commands.Review;
using Accounting.Application.Expenses.Queries.GetById;
using Accounting.Application.Expenses.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExpensesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("lists")]
    [ProducesResponseType(typeof(Accounting.Application.Expenses.Queries.Dto.ExpenseListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateList([FromBody] CreateExpenseListCommand cmd, CancellationToken ct)
    {
        var res = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetListById), new { id = res.Id }, res);
    }

    [HttpGet("lists/{id:int}")]
    [ProducesResponseType(typeof(Accounting.Application.Expenses.Queries.Dto.ExpenseListDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetListById([FromRoute] int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetExpenseListByIdQuery(id), ct);
        return Ok(res);
    }

    [HttpGet("lines/{id:int}")]
    public ActionResult GetLineById([FromRoute] int id) => Ok(new { id });

    [HttpGet("lines")]
    public async Task<ActionResult> ListLines(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? sort = "dateUtc:desc",
    [FromQuery] int? expenseListId = null,
    [FromQuery] int? supplierId = null,
    [FromQuery] string? currency = null,
    [FromQuery] string? category = null,
    [FromQuery] string? dateFromUtc = null,
    [FromQuery] string? dateToUtc = null,
    CancellationToken ct = default)
    {
        var res = await _mediator.Send(new ListExpensesQuery(
            pageNumber, pageSize, sort,
            expenseListId, supplierId, currency, category, dateFromUtc, dateToUtc
        ), ct);

        return Ok(res);
    }

    [HttpPost("lists/{id:int}/review")]
    [ProducesResponseType(typeof(Accounting.Application.Expenses.Queries.Dto.ExpenseListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReviewList([FromRoute] int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new ReviewExpenseListCommand(id), ct);
        return Ok(res);
    }
}
