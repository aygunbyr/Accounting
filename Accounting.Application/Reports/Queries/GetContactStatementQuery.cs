using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Reports.Queries;

public record GetContactStatementQuery(int ContactId, DateTime? DateFrom, DateTime? DateTo) : IRequest<ContactStatementDto>;

public class GetContactStatementHandler(IAppDbContext db) : IRequestHandler<GetContactStatementQuery, ContactStatementDto>
{
    public async Task<ContactStatementDto> Handle(GetContactStatementQuery request, CancellationToken ct)
    {
        var contact = await db.Contacts.FindAsync(new object[] { request.ContactId }, ct);
        if (contact == null) throw new ApplicationException("Contact not found");

        var fromDate = request.DateFrom ?? DateTime.MinValue;
        var toDate = request.DateTo ?? DateTime.MaxValue;

        // 1. Opening Balance (Devir Bakiyesi)
        // DateFrom'dan önceki tüm hareketlerin toplamı
        var prevInvoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.ContactId == request.ContactId && !i.IsDeleted && i.DateUtc < fromDate)
            .Select(i => new { i.Type, i.TotalGross })
            .ToListAsync(ct);

        var prevPayments = await db.Payments
            .AsNoTracking()
            .Where(p => p.ContactId == request.ContactId && !p.IsDeleted && p.DateUtc < fromDate)
            .Select(p => new { p.Direction, p.Amount })
            .ToListAsync(ct);

        decimal openingBalance = 0;
        
        // Fatura Etkisi: Satış (+) Borç Artırır, Alış (-) Alacak Artırır (Bizim açımızdan değil, cari bakiye açısından)
        // Cari Bakiye = (Satışlar - Alışlar) - (Tahsilatlar - Ödemeler) ?? 
        // Basit Bakiye: Borç - Alacak
        // Satış Faturası => Borç
        // Alış Faturası => Alacak
        // Ödeme (In - Tahsilat) => Alacak (Borç düşer)
        // Ödeme (Out - Tediye) => Borç (Alacak düşer)

        foreach (var inv in prevInvoices)
        {
            if (inv.Type == InvoiceType.Sales) openingBalance += inv.TotalGross; // Borç
            else openingBalance -= inv.TotalGross; // Alacak
        }

        foreach (var pay in prevPayments)
        {
            if (pay.Direction == PaymentDirection.In) openingBalance -= pay.Amount; // Alacak (Tahsilat)
            else openingBalance += pay.Amount; // Borç (Tediye)
        }

        // 2. Fetch Transactions in Range
        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.ContactId == request.ContactId && !i.IsDeleted && i.DateUtc >= fromDate && i.DateUtc <= toDate)
            .Select(i => new StatementTransaction
            {
                DateUtc = i.DateUtc,
                Type = i.Type == InvoiceType.Sales ? "Satış Faturası" : "Alış Faturası",
                DocNo = i.InvoiceNumber,
                Desc = "", // Description property doesn't exist on Invoice yet
                Debt = i.Type == InvoiceType.Sales ? i.TotalGross : 0,
                Credit = i.Type == InvoiceType.Purchase ? i.TotalGross : 0
            })
            .ToListAsync(ct);

        var payments = await db.Payments
            .AsNoTracking()
            .Where(p => p.ContactId == request.ContactId && !p.IsDeleted && p.DateUtc >= fromDate && p.DateUtc <= toDate)
            .Select(p => new StatementTransaction
            {
                DateUtc = p.DateUtc,
                Type = p.Direction == PaymentDirection.In ? "Tahsilat" : "Ödeme",
                DocNo = p.Id.ToString(), // Ödeme No
                Desc = "", // Payment might not have Description either, check entity if needed
                Debt = p.Direction == PaymentDirection.Out ? p.Amount : 0,
                Credit = p.Direction == PaymentDirection.In ? p.Amount : 0
            })
            .ToListAsync(ct);

        // 3. Merge and Sort
        var allTransactions = invoices.Concat(payments).OrderBy(x => x.DateUtc).ToList();

        // 4. Calculate Running Balance
        var resultItems = new List<StatementItemDto>();
        
        // Add Opening Balance Line
        if (fromDate > DateTime.MinValue)
        {
            resultItems.Add(new StatementItemDto(
                fromDate,
                "DEVİR",
                "-",
                "Önceki dönem bakiyesi",
                openingBalance > 0 ? openingBalance : 0, // Devir Borç
                openingBalance < 0 ? Math.Abs(openingBalance) : 0, // Devir Alacak
                openingBalance // Bakiye
            ));
        }

        decimal currentBalance = openingBalance;

        foreach (var txn in allTransactions)
        {
            currentBalance += txn.Debt;
            currentBalance -= txn.Credit;

            resultItems.Add(new StatementItemDto(
                txn.DateUtc,
                txn.Type,
                txn.DocNo,
                txn.Desc ?? "",
                txn.Debt,
                txn.Credit,
                currentBalance
            ));
        }

        return new ContactStatementDto(contact.Id, contact.Name, resultItems);
    }

    private class StatementTransaction
    {
        public DateTime DateUtc { get; set; }
        public required string Type { get; set; }
        public required string DocNo { get; set; }
        public string? Desc { get; set; }
        public decimal Debt { get; set; }
        public decimal Credit { get; set; }
    }
}
