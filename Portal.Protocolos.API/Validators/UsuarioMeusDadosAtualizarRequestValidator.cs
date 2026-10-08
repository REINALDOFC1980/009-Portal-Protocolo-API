using FluentValidation;
using Portal.Protocolos.API.Models.Seguranca.Usuario;

namespace Portal.Protocolos.API.Validators;

public sealed class UsuarioMeusDadosAtualizarRequestValidator
    : AbstractValidator<UsuarioMeusDadosAtualizarRequest>
{
    public UsuarioMeusDadosAtualizarRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("Informe o nome ou razão social.")
            .MaximumLength(150);

        RuleFor(x => x.NomeFantasia)
            .MaximumLength(150);

        RuleFor(x => x.TipoPessoaJuridicaId)
            .Must(tipo => tipo is null or 1 or 2)
            .WithMessage("Tipo de pessoa jurídica inválido.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .EmailAddress()
            .WithMessage("E-mail inválido.")
            .MaximumLength(200);

        RuleFor(x => x.Celular)
            .MaximumLength(20);

        RuleFor(x => x.RegistroCnh)
            .MaximumLength(20)
            .WithMessage(
                "O número da CNH deve possuir no máximo 20 caracteres.");

        RuleFor(x => x.UfCnh)
            .Must(uf => uf is not null && uf.Trim().Length == 2)
            .WithMessage("Informe uma UF válida para a CNH.")
            .When(x => !string.IsNullOrWhiteSpace(x.UfCnh));

        RuleFor(x => x.ModeloCnh)
            .Must(modelo => modelo is 2 or 3)
            .WithMessage("Modelo da CNH inválido.")
            .When(x => x.ModeloCnh.HasValue);

        RuleFor(x => x)
            .Custom((request, contexto) =>
            {
                var possuiRegistro =
                    !string.IsNullOrWhiteSpace(request.RegistroCnh);

                var possuiUf =
                    !string.IsNullOrWhiteSpace(request.UfCnh);

                var possuiModelo =
                    request.ModeloCnh.HasValue;

                if (!possuiRegistro && !possuiUf && !possuiModelo)
                {
                    return;
                }

                if (!possuiRegistro)
                {
                    contexto.AddFailure(
                        nameof(request.RegistroCnh),
                        "Informe o número da CNH.");
                }

                if (!possuiUf)
                {
                    contexto.AddFailure(
                        nameof(request.UfCnh),
                        "Informe a UF da CNH.");
                }

                if (!possuiModelo)
                {
                    contexto.AddFailure(
                        nameof(request.ModeloCnh),
                        "Informe o modelo da CNH.");
                }
            });
    }
}
