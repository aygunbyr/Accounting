using Accounting.Application.StockMovements.Commands.Transfer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[Route("api/stock-transfers")]
[ApiController]
public class StockTransfersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Transfer(TransferStockCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }
}
