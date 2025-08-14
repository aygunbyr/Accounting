using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using Accounting.Application.Payments.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounting.Application.Payments.Queries.List
{
    public class ListPaymentsHandler : IRequestHandler<ListPaymentsQuery, PagedResult<PaymentListItemDto>>
    {
        private readonly IAppDbContext _db;
        public ListPaymentsHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<PaymentListItemDto>> Handle(ListPaymentsQuery q, CancellationToken ct)
        {
            var query = _db.Payments.AsNoTracking();

            // Filtreler
            if (q.AccountId is int accId)
                query = query.Where(p => p.AccountId == accId);

            if (q.ContactId is int conId)
                query = query.Where(p => p.ContactId == conId);

            if (q.Direction is not null)
                query = query.Where(p => p.Direction == q.Direction);

            if (TryParseUtc(q.DateFromUtc, out var fromUtc)) query = query.Where(p => p.DateUtc >= fromUtc);
            if (TryParseUtc(q.DateToUtc, out var toUtc)) query = query.Where(p => p.DateUtc <= toUtc);

            // Sıralama
            var sort = (q.Sort ?? "dateUtc:desc").Split(":");
            var field = sort[0].ToLowerInvariant();
            var dir = sort.Length > 1 ? sort[1].ToLowerInvariant() : "desc";

            query = (field, dir) switch
            {
                ("amount", "asc") => query.OrderBy(p => p.Amount),
                ("amount", "desc") => query.OrderByDescending(p => p.Amount),
                ("dateutc", "asc") => query.OrderBy(p => p.DateUtc),
                _ => query.OrderByDescending(p => p.DateUtc),
            };

            var total = await query.CountAsync(ct);

            var page = await query
                .Skip((q.PageNumber - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.AccountId,
                    p.ContactId,
                    p.LinkedInvoiceId,
                    p.DateUtc,
                    p.Direction,
                    p.Amount,
                    p.Currency
                })
                .ToListAsync(ct);

            var inv = CultureInfo.InvariantCulture;
            var items = page.Select(p => new PaymentListItemDto(
                p.Id,
                p.AccountId,
                p.ContactId,
                p.LinkedInvoiceId,
                p.DateUtc,
                p.Direction.ToString(),
                p.Amount.ToString("F2", inv),
                p.Currency

                )).ToList();

            return new PagedResult<PaymentListItemDto>(total, q.PageNumber, q.PageSize, items);

        }

        private static bool TryParseUtc(string? s, out DateTime value)
            => DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out value);
    }
}