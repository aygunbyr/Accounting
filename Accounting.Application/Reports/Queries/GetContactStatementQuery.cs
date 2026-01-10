using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Reports.Queries;

public record GetContactStatementQuery(int ContactId, DateTime? DateFrom, DateTime? DateTo) : IRequest<ContactStatementDto>;

public class GetContactStatementHandler : IRequestHandler<GetContactStatementQuery, ContactStatementDto>
{
    private readonly IAppDbContext _db;
    private readonly IContactBalanceService _balanceService;

    public GetContactStatementHandler(IAppDbContext db, IContactBalanceService balanceService)
    {
        _db = db;
        _balanceService = balanceService;
    }

    public async Task<ContactStatementDto> Handle(GetContactStatementQuery request, CancellationToken ct)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { request.ContactId }, ct);
        if (contact == null || contact.IsDeleted)
            throw new NotFoundException("Contact", request.ContactId);

        var fromDate = request.DateFrom ?? DateTime.MinValue;
        var toDate = request.DateTo ?? DateTime.MaxValue;

        // 1. Opening Balance (Devir Bakiyesi) - Servis kullanarak
        var openingBalance = fromDate > DateTime.MinValue
            ? await _balanceService.CalculateBalanceAsync(request.ContactId, fromDate, ct)
            : 0m;

        // 2. Fetch Transactions in Range - Servis kullanarak
        var transactions = await _balanceService.GetTransactionsAsync(request.ContactId, fromDate, toDate, ct);

        // 3. Build Result with Running Balance
        var resultItems = new List<StatementItemDto>();

        // Add Opening Balance Line
        if (fromDate > DateTime.MinValue)
        {
            resultItems.Add(new StatementItemDto(
                fromDate,
                "DEVİR",
                "-",
                "Önceki dönem bakiyesi",
                openingBalance > 0 ? openingBalance : 0,
                openingBalance < 0 ? Math.Abs(openingBalance) : 0,
                openingBalance
            ));
        }

        decimal currentBalance = openingBalance;

        foreach (var txn in transactions)
        {
            currentBalance += txn.Debt;
            currentBalance -= txn.Credit;

            resultItems.Add(new StatementItemDto(
                txn.DateUtc,
                txn.Type,
                txn.DocNo,
                txn.Description ?? "",
                txn.Debt,
                txn.Credit,
                currentBalance
            ));
        }

        return new ContactStatementDto(contact.Id, contact.Name, resultItems);
    }
}
