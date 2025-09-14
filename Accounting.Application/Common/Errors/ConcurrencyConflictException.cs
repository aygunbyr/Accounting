namespace Accounting.Application.Common.Errors;

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message = "Kaynak başka biri tarafından değiştirildi.")
        : base("concurrency_conflict", message) { }
}
