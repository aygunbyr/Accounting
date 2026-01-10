namespace Accounting.Application.CompanySettings.Dto;

public record CompanySettingsDto(
    int Id,
    string Title,
    string? TaxNumber,
    string? TaxOffice,
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? TradeRegisterNo,
    string? MersisNo,
    string? LogoUrl,
    string? RowVersionBase64
);
