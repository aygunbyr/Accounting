using Accounting.Application.Orders.Commands.Approve;
using Accounting.Application.Orders.Commands.Create;
using Accounting.Application.Orders.Commands.CreateInvoice;
using Accounting.Application.Orders.Commands.Delete;
using Accounting.Application.Orders.Commands.Update;
using Accounting.Application.Orders.Dto;
using Accounting.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[Route("api/orders")]
[ApiController]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetList()
    {
        return Ok(await mediator.Send(new GetOrdersQuery()));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        return Ok(await mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OrderDto>> Update(int id, UpdateOrderCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        return Ok(await mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(int id, [FromQuery] string rowVersion)
    {
        return Ok(await mediator.Send(new DeleteOrderCommand(id, rowVersion)));
    }

    [HttpPut("{id}/approve")]
    public async Task<ActionResult<bool>> Approve(int id, [FromQuery] string rowVersion)
    {
        return Ok(await mediator.Send(new ApproveOrderCommand(id, rowVersion)));
    }

    [HttpPost("{id}/create-invoice")]
    public async Task<ActionResult<int>> CreateInvoice(int id)
    {
        // rowVersion check might be needed if we want to ensure order hasn't changed, 
        // but CreateInvoiceFromOrderHandler checks Status=Approved, so it's relatively safe.
        // For strictness, we could add rowVersion to the command too.
        return Ok(await mediator.Send(new CreateInvoiceFromOrderCommand(id)));
    }
}
