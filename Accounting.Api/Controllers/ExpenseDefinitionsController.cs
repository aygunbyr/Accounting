using Accounting.Application.Common.Models;
using Accounting.Application.ExpenseDefinitions.Queries.Dto;
using Accounting.Application.ExpenseDefinitions.Queries.GetById;
using Accounting.Application.ExpenseDefinitions.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseDefinitionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExpenseDefinitionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/ExpenseDefinitions
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ExpenseDefinitionListItemDto>>> List(
            [FromQuery] ListExpenseDefinitionsQuery query,
            CancellationToken ct)
        {
            var res = await _mediator.Send(query, ct);
            return Ok(res);
        }

        // GET api/ExpenseDefinitions/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExpenseDefinitionDetailDto>> GetById(
            [FromRoute] int id,
            CancellationToken ct)
        {
            if (id <= 0) return BadRequest();

            var res = await _mediator.Send(new GetExpenseDefinitionByIdQuery(id), ct);
            return Ok(res);
        }
    }
}
