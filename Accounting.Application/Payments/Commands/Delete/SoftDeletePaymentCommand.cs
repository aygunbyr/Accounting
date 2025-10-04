using Accounting.Application.Common.Abstractions;
using MediatR;

public record SoftDeletePaymentCommand(
    int Id,
    string RowVersion
) : IRequest, ITransactionalRequest;
