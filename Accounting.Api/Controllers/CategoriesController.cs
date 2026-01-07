using Accounting.Application.Categories.Commands.Create;
using Accounting.Application.Categories.Commands.Delete;
using Accounting.Application.Categories.Commands.Update;
using Accounting.Application.Categories.Queries;
using Accounting.Application.Categories.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetList(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCategoriesQuery(), ct));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, UpdateCategoryCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(int id, [FromQuery] string rowVersion, CancellationToken ct)
    {
        return Ok(await mediator.Send(new DeleteCategoryCommand(id, rowVersion), ct));
    }
}
