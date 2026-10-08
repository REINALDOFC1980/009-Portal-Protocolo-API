using FluentValidation;
using Portal.Protocolos.API.Models.Seguranca.Usuario;

namespace Portal.Protocolos.API.Validators;

public sealed class UsuarioCadastroRequestValidator
    : AbstractValidator<UsuarioCadastroRequest>
{
    public UsuarioCadastroRequestValidator()
    {
        RuleFor(x => x.Documento)
            .NotEmpty()
            .WithMessage(
                "Informe o CPF ou CNPJ.")
            .Must(DocumentoValido)
            .WithMessage(
                "Informe um CPF ou CNPJ válido.");

        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage(
                "Informe o nome ou razão social.")
            .MaximumLength(150);

        RuleFor(x => x.NomeFantasia)
            .MaximumLength(150);

        RuleFor(x => x.TipoPessoaJuridicaId)
            .NotNull()
            .WithMessage(
                "Informe o tipo da pessoa jurídica.")
            .Must(x => x is 1 or 2)
            .WithMessage(
                "Tipo de pessoa jurídica inválido.")
            .When(x =>
                SomenteNumeros(
                    x.Documento).Length == 14);

        RuleFor(x => x.TipoPessoaJuridicaId)
            .Null()
            .WithMessage(
                "O tipo de pessoa jurídica não deve ser informado para pessoa física.")
            .When(x =>
                SomenteNumeros(
                    x.Documento).Length == 11);

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(
                "Informe o e-mail.")
            .EmailAddress()
            .WithMessage(
                "E-mail inválido.")
            .MaximumLength(200);

        RuleFor(x => x.Celular)
            .MaximumLength(20);

        RuleFor(x => x.RegistroCnh)
            .MaximumLength(20)
            .WithMessage(
                "O número da CNH deve possuir no máximo 20 caracteres.")
            .When(x =>
                SomenteNumeros(
                    x.Documento).Length == 11);

        RuleFor(x => x.UfCnh)
            .Length(2)
            .WithMessage(
                "Informe uma UF válida para a CNH.")
            .When(x =>
                SomenteNumeros(
                    x.Documento).Length == 11 &&
                !string.IsNullOrWhiteSpace(
                    x.UfCnh));

        RuleFor(x => x.ModeloCnh)
            .Must(modelo =>
                modelo is 2 or 3)
            .WithMessage(
                "Modelo da CNH inválido.")
            .When(x =>
                SomenteNumeros(
                    x.Documento).Length == 11 &&
                x.ModeloCnh.HasValue);

        RuleFor(x => x)
            .Custom(ValidarCnh);

        RuleFor(x => x.Senha)
            .NotEmpty()
            .WithMessage(
                "Informe a senha.")
            .MinimumLength(8)
            .WithMessage(
                "A senha deve possuir no mínimo 8 caracteres.");

        RuleFor(x => x.ConfirmarSenha)
            .Equal(x => x.Senha)
            .WithMessage(
                "As senhas não conferem.");
    }

    private static void ValidarCnh(
        UsuarioCadastroRequest request,
        ValidationContext<UsuarioCadastroRequest> contexto)
    {
        var pessoaFisica =
            SomenteNumeros(
                request.Documento).Length == 11;

        if (!pessoaFisica)
        {
            return;
        }

        var possuiRegistro =
            !string.IsNullOrWhiteSpace(
                request.RegistroCnh);

        var possuiUf =
            !string.IsNullOrWhiteSpace(
                request.UfCnh);

        var possuiModelo =
            request.ModeloCnh.HasValue;

        var informouAlgumDado =
            possuiRegistro ||
            possuiUf ||
            possuiModelo;

        if (!informouAlgumDado)
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
    }

    private static bool DocumentoValido(
        string? documento)
    {
        var numeros =
            SomenteNumeros(
                documento);

        return numeros.Length is 11 or 14;
    }

    private static string SomenteNumeros(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return string.Empty;
        }

        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
