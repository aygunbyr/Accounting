using Accounting.Application.Items.Queries.Dto;
using MediatR;

namespace Accounting.Application.Items.Commands.Create;

public record CreateItemCommand(
    int? CategoryId,
    string Code,
    string Name,
    string Unit,
    int VatRate,             // 0..100
    string? PurchasePrice,   // string money
    string? SalesPrice       // string money
) : IRequest<ItemDetailDto>;
