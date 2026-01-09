namespace Accounting.Application.Reports.Queries;

public record ContactStatementDto(
    int ContactId,
    string ContactName,
    List<StatementItemDto> Items
);

public record StatementItemDto(
    DateTime DateUtc,
    string Type,        // "Fatura", "Tahsilat", "Ödeme"
    string DocumentNo,
    string Description,
    decimal Debt,       // Borç (Müşteri Borçlandı / Biz Mal Sattık)
    decimal Credit,     // Alacak (Müşteri Ödedi / Biz Mal Aldık)
    decimal Balance     // Bakiye (Borç - Alacak)
);
