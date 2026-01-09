using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Cheques.Commands.UpdateStatus;

public record UpdateChequeStatusCommand(
    int Id, 
    ChequeStatus NewStatus, 
    DateTime? TransactionDate = null,
    int? CashBankAccountId = null // Tahsilat/Ödeme için gerekli
) : IRequest;

public class UpdateChequeStatusHandler(IAppDbContext db) : IRequestHandler<UpdateChequeStatusCommand>
{
    public async Task Handle(UpdateChequeStatusCommand request, CancellationToken ct)
    {
        var cheque = await db.Cheques
            .Include(c => c.Contact)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (cheque == null) throw new ApplicationException("Çek/Senet bulunamadı");

        // Status Validations
        if (cheque.Status == ChequeStatus.Paid)
            throw new ApplicationException("Zaten ödenmiş/tahsil edilmiş evrakın durumu değiştirilemez.");

        if (cheque.Status == request.NewStatus)
            return; // No change

        // Workflow Logic
        if (request.NewStatus == ChequeStatus.Paid)
        {
            if (!request.CashBankAccountId.HasValue)
                throw new ApplicationException("Tahsilat/Ödeme işlemi için Kasa/Banka hesabı seçilmelidir.");

            await ProcessPaymentAsync(cheque, request.CashBankAccountId.Value, request.TransactionDate ?? DateTime.UtcNow, ct);
        }
        else if (request.NewStatus == ChequeStatus.Bounced)
        {
             // Karşılıksız durumu (Sadece işaretleme, ekstra muhasebe kaydı şu an yok)
        }

        cheque.Status = request.NewStatus;
        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessPaymentAsync(Cheque cheque, int accountId, DateTime dateUtc, CancellationToken ct)
    {
        var direction = cheque.Direction == ChequeDirection.Inbound 
            ? PaymentDirection.In  // Müşteri çeki tahsilatı -> Kasa Giriş
            : PaymentDirection.Out; // Kendi çekimiz ödenmesi -> Kasa Çıkış

        var payment = new Payment
        {
            BranchId = cheque.BranchId,
            AccountId = accountId,
            ContactId = cheque.ContactId,
            Direction = direction,
            Amount = cheque.Amount,
            Currency = cheque.Currency,
            DateUtc = dateUtc,
            // Link? Payment entity'sinde ChequeId yok. Description'a yazacağız veya Payment Entity'si güncellenmeli.
            // MVP için Description'a yazıyoruz.
        };

        db.Payments.Add(payment);
    }
}
