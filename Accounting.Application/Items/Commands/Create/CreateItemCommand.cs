using Accounting.Application.Items.Queries.Dto;
using MediatR;

namespace Accounting.Application.Items.Commands.Create;

public record CreateItemCommand(
    string Name,
    string Unit,
    int VatRate,             // 0..100
    string? DefaultUnitPrice // string money veya null
) : IRequest<ItemDetailDto>;
