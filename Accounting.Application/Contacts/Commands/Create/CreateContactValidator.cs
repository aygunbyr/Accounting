using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Contacts.Commands.Create;

public class CreateContactValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        
        // Common
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Type == ContactIdentityType.Company);

        // Company Validation
        RuleFor(x => x.CompanyDetails).NotNull().When(x => x.Type == ContactIdentityType.Company);
        RuleFor(x => x.CompanyDetails!.TaxNumber).NotEmpty().Length(10).When(x => x.Type == ContactIdentityType.Company && x.CompanyDetails != null);
        RuleFor(x => x.CompanyDetails!.TaxOffice).NotEmpty().MaximumLength(100).When(x => x.Type == ContactIdentityType.Company && x.CompanyDetails != null);
        RuleFor(x => x.CompanyDetails!.MersisNo).MaximumLength(20).When(x => x.Type == ContactIdentityType.Company && x.CompanyDetails != null);
        RuleFor(x => x.CompanyDetails!.TicaretSicilNo).MaximumLength(20).When(x => x.Type == ContactIdentityType.Company && x.CompanyDetails != null);

        // Person Validation
        RuleFor(x => x.PersonDetails).NotNull().When(x => x.Type == ContactIdentityType.Person);
        RuleFor(x => x.PersonDetails!.Tckn).NotEmpty().Length(11).When(x => x.Type == ContactIdentityType.Person && x.PersonDetails != null);
        RuleFor(x => x.PersonDetails!.FirstName).NotEmpty().MaximumLength(100).When(x => x.Type == ContactIdentityType.Person && x.PersonDetails != null);
        RuleFor(x => x.PersonDetails!.LastName).NotEmpty().MaximumLength(100).When(x => x.Type == ContactIdentityType.Person && x.PersonDetails != null);
        RuleFor(x => x.PersonDetails!.Title).MaximumLength(100).When(x => x.Type == ContactIdentityType.Person && x.PersonDetails != null);
        RuleFor(x => x.PersonDetails!.Department).MaximumLength(100).When(x => x.Type == ContactIdentityType.Person && x.PersonDetails != null);

        // Employee MUST be Person
        RuleFor(x => x.Type).Equal(ContactIdentityType.Person).When(x => x.IsEmployee)
            .WithMessage("Personel (Employee) kaydı mutlaka Şahıs (Person) tipinde olmalıdır.");
    }
}
