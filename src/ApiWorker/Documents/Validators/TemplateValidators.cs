using FluentValidation;
using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.Validators;

/// <summary>
/// Validates template listing requests.
/// </summary>
public sealed class ListTemplatesRequestValidator : AbstractValidator<ListTemplatesRequest>
{
    public ListTemplatesRequestValidator()
    {
        // Type filter is optional, but if provided must be valid enum value
        When(x => x.Type.HasValue, () =>
        {
            RuleFor(x => x.Type!.Value)
                .IsInEnum()
                .WithMessage("Document type must be Invoice, Receipt, or Quotation");
        });
    }
}

