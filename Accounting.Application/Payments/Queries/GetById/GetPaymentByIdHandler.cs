using Accounting.Application.Common.Abstractions;
using Accounting.Application.Payments.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Payments.Queries.GetById;

public class GetPaymentByIdHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDetailDto>
{
    private readonly IAppDbContext _db;
    public GetPaymentByIdHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDetailDto> Handle(GetPaymentByIdQuery q, CancellationToken ct)
    {
        var p = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (p is null)
            throw new KeyNotFoundException($"Payment {q.Id} not found.");

        var inv = CultureInfo.InvariantCulture;

        return new PaymentDetailDto(
            p.Id,
            p.AccountId,
            p.ContactId,
            p.LinkedInvoiceId,
            p.DateUtc,
            p.Direction.ToString(),
            p.Amount.ToString("F2", inv),
            p.Currency
            );
    }
}
