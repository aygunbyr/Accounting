using Accounting.Application.Payments.Commands.Create;
using Accounting.Application.Payments.Queries.GetById;
using Accounting.Application.Payments.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePaymentResult>> Create([FromBody] CreatePaymentCommand cmd, CancellationToken ct)
    {
        var res = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetPaymentByIdQuery(id), ct);
        return Ok(res);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "dateUtc:desc",
        [FromQuery] int? accountId = null,
        [FromQuery] int? contactId = null,
        [FromQuery] int? direction = null,       // 1=In, 2=Out
        [FromQuery] string? dateFromUtc = null,  // ISO-8601
        [FromQuery] string? dateToUtc = null,    // ISO-8601
        CancellationToken ct = default)
    {
        var dirEnum = direction is null ? null
            : (Accounting.Domain.Entities.PaymentDirection?)direction;

        var res = await _mediator.Send(new ListPaymentsQuery(
            pageNumber, pageSize, sort,
            accountId, contactId, dirEnum,
            dateFromUtc, dateToUtc
        ), ct);

        return Ok(res);
    }
}
