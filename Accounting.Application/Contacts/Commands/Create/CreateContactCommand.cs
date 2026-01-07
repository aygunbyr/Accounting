using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Contacts.Commands.Create;

public record CreateContactCommand(
    int BranchId,
    string Name,
    ContactType Type,
    string? Email
    ) : IRequest<ContactDto>;
