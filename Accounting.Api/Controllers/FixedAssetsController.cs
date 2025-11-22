using Accounting.Application.Common.Models;
using Accounting.Application.FixedAssets.Queries.Dto;
using Accounting.Application.FixedAssets.Queries.GetById;
using Accounting.Application.FixedAssets.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class FixedAssetsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FixedAssetsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/FixedAssets
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<FixedAssetListItemDto>>> List(
            [FromQuery] ListFixedAssetsQuery query,
            CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        // GET api/FixedAssets/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FixedAssetDetailDto>> GetById(
            [FromRoute] int id,
            CancellationToken ct)
        {
            if (id <= 0) return BadRequest();

            var result = await _mediator.Send(new GetFixedAssetByIdQuery(id), ct);
            return Ok(result);
        }
    }
}
