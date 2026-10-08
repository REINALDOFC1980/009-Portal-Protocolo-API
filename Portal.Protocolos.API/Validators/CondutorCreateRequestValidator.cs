using FluentValidation;
using Portal.Protocolos.API.Models.Atendimento.Condutor;

namespace Portal.Protocolos.API.Validators;

public sealed class CondutorCreateRequestValidator
    : AbstractValidator<CondutorCreateRequest>
{
    public CondutorCreateRequestValidator()
    {
        RuleFor(x => x.DocumentoCondutor)
            .NotEmpty()
            .WithMessage("Informe o CPF do condutor.")
            .Length(11)
            .WithMessage("O CPF do condutor deve possuir 11 dígitos.");

        RuleFor(x => x.Auto)
            .NotNull()
            .WithMessage("Informe os dados do AIT.");

        RuleFor(x => x.Auto.NumeroAuto)
            .NotEmpty()
            .When(x => x.Auto is not null)
            .WithMessage("Informe o número do AIT.");
    }
}
