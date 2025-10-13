using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Models;
using MediatR;

namespace Accounting.Application.CashBankAccounts.Queries.List;

public enum CashBankAccountTypeFilter { Any = 0, Cash = 1, Bank = 2 }

public record ListCashBankAccountsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Sort = "name:asc",
    CashBankAccountTypeFilter Type = CashBankAccountTypeFilter.Any,
    string? Search = null,
    string? IbanStartsWith = null
) : IRequest<PagedResult<CashBankAccountListItemDto>>;
