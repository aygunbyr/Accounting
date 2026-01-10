using Accounting.Application.Items.Queries.Dto;
using MediatR;
using System.Data;

namespace Accounting.Application.Items.Commands.Update;

public record UpdateItemCommand(
    int Id,
    int? CategoryId,
    string Name,
    string Unit,
    int VatRate,
    string? PurchasePrice,
    string? SalesPrice,
    string RowVersion   // base64
) : IRequest<ItemDetailDto>;
