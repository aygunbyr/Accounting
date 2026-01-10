namespace Accounting.Application.Cheques.Queries.Dto;

public record ChequeDetailDto(
    int Id,
    int BranchId,
    string ChequeNumber,
    string Type,
    decimal Amount,
    DateTime DueDateUtc,
    string? DrawerName,
    string? BankName,
    string Status,
    DateTime CreatedAtUtc
);
