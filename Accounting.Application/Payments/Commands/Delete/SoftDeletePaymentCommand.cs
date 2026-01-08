using MediatR;

public record SoftDeletePaymentCommand(
    int Id,
    string RowVersion
) : IRequest;
