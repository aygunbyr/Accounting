namespace Accounting.Application.Common.Errors;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message)
        : base("business_rule_violation", message) { }
}