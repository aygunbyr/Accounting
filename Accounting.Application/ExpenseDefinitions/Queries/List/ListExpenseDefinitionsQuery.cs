using Accounting.Application.Common.Models;
using Accounting.Application.Common.Constants;
using Accounting.Application.ExpenseDefinitions.Queries.Dto;
using MediatR;

namespace Accounting.Application.ExpenseDefinitions.Queries.List;

public record ListExpenseDefinitionsQuery(
    int PageNumber = 1,
    int PageSize = PaginationConstants.DefaultPageSize,
    string? Search = null,      // Code veya Name contains (case-insensitive)
    bool? OnlyActive = true     // null: hepsi, true: sadece aktif, false: sadece pasif
) : IRequest<PagedResult<ExpenseDefinitionListItemDto>>;