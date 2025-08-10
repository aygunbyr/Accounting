using Accounting.Api.Contracts.Invoices;
using Accounting.Application.Invoices.Commands.Create;
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
    public async Task<ActionResult<CreateInvoiceResult>> Create([FromBody] CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var res = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id, CancellationToken ct)
    {
        // Şimdilik basit (ileride Query nesnesi yaparız)
        return Ok(new { id });
    }
}
