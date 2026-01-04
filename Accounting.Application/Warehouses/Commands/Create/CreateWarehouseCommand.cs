using Accounting.Application.Warehouses.Dto;
using MediatR;

namespace Accounting.Application.Warehouses.Commands.Create;

public record CreateWarehouseCommand(
    int BranchId,
    string Code,
    string Name,
    bool IsDefault
) : IRequest<WarehouseDto>;
