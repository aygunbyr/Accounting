using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Contacts.Commands.Update;

public record UpdateContactCommand(
    int Id,
    string Name,
    ContactType Type,
    string? Email,
    string RowVersion // base64
    ) : IRequest<ContactDto>, ITransactionalRequest;
