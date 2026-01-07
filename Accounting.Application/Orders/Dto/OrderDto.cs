using Accounting.Domain.Enums;

namespace Accounting.Application.Orders.Dto;

public record OrderDto(
    int Id,
    int? OrderNumber, // Derived or stored? Stored as string, but maybe simplified for DTO
    string OrderNo,
    int ContactId,
    string ContactName,
    DateTime DateUtc,
    OrderStatus Status,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    string Currency,
    string? Description,
    List<OrderLineDto> Lines,
    DateTime CreatedAtUtc,
    string RowVersion
);

public record OrderLineDto(
    int Id,
    int? ItemId,
    string? ItemName,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    int VatRate,
    decimal Total
);
