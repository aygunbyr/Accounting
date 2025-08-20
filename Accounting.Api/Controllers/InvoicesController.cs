﻿using Accounting.Api.Contracts.Invoices;
using Accounting.Application.Invoices.Commands.Create;
using Accounting.Application.Invoices.Queries.GetById;
using Accounting.Application.Invoices.Queries.List;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(CreateInvoiceResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateInvoiceResult>> Create([FromBody] CreateInvoiceCommand command, CancellationToken ct)
    {
        var res = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return Ok(res);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "dateUtc:desc",
        [FromQuery] int? contactId = null,
        [FromQuery] int? type = 0, // 0 Any, 1 Sales, 2 Purchase
        [FromQuery] string? dateFromUtc = null,
        [FromQuery] string? dateToUtc = null,
        CancellationToken ct = default)
    {
        var typeEnum = Enum.IsDefined(typeof(InvoiceTypeFilter), type ?? 0)
            ? (InvoiceTypeFilter)(type ?? 0)
            : InvoiceTypeFilter.Any;

        var res = await _mediator.Send(new ListInvoicesQuery(
            pageNumber, pageSize, sort,
            contactId, typeEnum, dateFromUtc, dateToUtc
        ), ct);

        return Ok(res);
    }
}
