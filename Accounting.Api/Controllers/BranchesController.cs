using Accounting.Application.Branches.Queries.Dto;
using Accounting.Application.Branches.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BranchesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/branches
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BranchDto>>> List(CancellationToken ct)
        {
            var res = await _mediator.Send(new ListBranchesQuery(), ct);
            return Ok(res);
        }
    }
}
