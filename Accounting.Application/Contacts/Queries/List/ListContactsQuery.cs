using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Contacts.Queries.List;

public record ListContactsQuery(
    int? BranchId,
    string? Search,
    ContactType? Type,
    int Page = 1,
    int PageSize = 20
    ) : IRequest<ContactListResult>;