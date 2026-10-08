using FluentValidation;
using Portal.Protocolos.API.Models.Seguranca.Login;

namespace Portal.Protocolos.API.Validators;

public sealed class LoginRequestValidator
    : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Documento)
            .NotEmpty()
            .WithMessage("Informe o CPF ou CNPJ.");

        RuleFor(x => x.Senha)
            .NotEmpty()
            .WithMessage("Informe a senha.");
    }
}
