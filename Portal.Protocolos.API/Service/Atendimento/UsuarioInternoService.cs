using Portal.Protocolos.API.Models.Atendimento.Administracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;
using System.Net.Mail;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class UsuarioInternoService
{
    private static readonly HashSet<string> PerfisPermitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ATENDENTE",
            "ANALISTA",
            "SUPERVISOR",
            "ADMINISTRADOR"
        };

    private readonly UsuarioInternoRepository _repository;

    public UsuarioInternoService(
        UsuarioInternoRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<UsuarioInternoResponse>> ListarAsync() =>
        _repository.ListarAsync();

    public async Task<int> CriarAsync(
        CriarUsuarioInternoRequest request)
    {
        var normalizado = NormalizarCriacao(request);
        ValidarSenha(normalizado.Senha);

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(
            normalizado.Senha,
            workFactor: 12);

        return await _repository.CriarOuVincularAsync(
            normalizado,
            senhaHash);
    }

    public async Task AtualizarAsync(
        int usuarioId,
        int usuarioAdministradorId,
        AtualizarUsuarioInternoRequest request)
    {
        if (usuarioId <= 0)
        {
            throw new BusinessException(
                "Usuário interno inválido.");
        }

        var normalizado = NormalizarAtualizacao(request);

        if (usuarioId == usuarioAdministradorId &&
            (!normalizado.Ativo ||
             !string.Equals(
                 normalizado.Perfil,
                 "ADMINISTRADOR",
                 StringComparison.Ordinal)))
        {
            throw new BusinessException(
                "Você não pode remover o seu próprio acesso de Administrador.");
        }

        await _repository.AtualizarAsync(
            usuarioId,
            normalizado);
    }

    public async Task AlterarStatusAsync(
        int usuarioId,
        int usuarioAdministradorId,
        AlterarStatusUsuarioInternoRequest request)
    {
        var perfil = NormalizarPerfil(request.Perfil);

        if (usuarioId == usuarioAdministradorId &&
            !request.Ativo)
        {
            throw new BusinessException(
                "Você não pode inativar o seu próprio acesso interno.");
        }

        await _repository.AlterarStatusAsync(
            usuarioId,
            perfil,
            request.Ativo);
    }

    public async Task RedefinirSenhaAsync(
        int usuarioId,
        RedefinirSenhaUsuarioInternoRequest request)
    {
        if (usuarioId <= 0)
        {
            throw new BusinessException(
                "Usuário interno inválido.");
        }

        ValidarSenha(request.NovaSenha);

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(
            request.NovaSenha,
            workFactor: 12);

        await _repository.RedefinirSenhaAsync(
            usuarioId,
            senhaHash);
    }

    private static CriarUsuarioInternoRequest NormalizarCriacao(
        CriarUsuarioInternoRequest request)
    {
        var cpf = SomenteNumeros(request.Cpf);
        var nome = request.Nome?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var celular = SomenteNumeros(request.Celular);
        var perfil = NormalizarPerfil(request.Perfil);

        ValidarPessoa(
            cpf,
            nome,
            email,
            celular);

        return new CriarUsuarioInternoRequest
        {
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Celular = string.IsNullOrWhiteSpace(celular)
                ? null
                : celular,
            Perfil = perfil,
            Senha = request.Senha ?? string.Empty
        };
    }

    private static AtualizarUsuarioInternoRequest NormalizarAtualizacao(
        AtualizarUsuarioInternoRequest request)
    {
        var nome = request.Nome?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var celular = SomenteNumeros(request.Celular);
        var perfil = NormalizarPerfil(request.Perfil);

        if (nome.Length is < 3 or > 150)
        {
            throw new BusinessException(
                "Informe o nome completo (3 a 150 caracteres).");
        }

        ValidarEmail(email);

        if (!string.IsNullOrWhiteSpace(celular) &&
            celular.Length is < 10 or > 11)
        {
            throw new BusinessException(
                "Informe um celular válido com DDD.");
        }

        return new AtualizarUsuarioInternoRequest
        {
            Nome = nome,
            Email = email,
            Celular = string.IsNullOrWhiteSpace(celular)
                ? null
                : celular,
            Perfil = perfil,
            Ativo = request.Ativo
        };
    }

    private static void ValidarPessoa(
        string cpf,
        string nome,
        string email,
        string celular)
    {
        if (cpf.Length != 11 ||
            cpf.Distinct().Count() == 1 ||
            !CpfValido(cpf))
        {
            throw new BusinessException(
                "Informe um CPF válido.");
        }

        if (nome.Length is < 3 or > 150)
        {
            throw new BusinessException(
                "Informe o nome completo (3 a 150 caracteres).");
        }

        ValidarEmail(email);

        if (!string.IsNullOrWhiteSpace(celular) &&
            celular.Length is < 10 or > 11)
        {
            throw new BusinessException(
                "Informe um celular válido com DDD.");
        }
    }

    private static void ValidarEmail(string email)
    {
        if (email.Length is < 5 or > 200 ||
            !MailAddress.TryCreate(email, out var endereco) ||
            !string.Equals(
                endereco.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Informe um e-mail válido.");
        }
    }

    private static string NormalizarPerfil(string? perfil)
    {
        var valor = (perfil ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (!PerfisPermitidos.Contains(valor))
        {
            throw new BusinessException(
                "Perfil interno inválido.");
        }

        return valor;
    }

    private static void ValidarSenha(string? senha)
    {
        if (string.IsNullOrEmpty(senha) ||
            senha.Length is < 8 or > 100)
        {
            throw new BusinessException(
                "A senha deve possuir entre 8 e 100 caracteres.");
        }
    }

    private static string SomenteNumeros(string? valor) =>
        new((valor ?? string.Empty)
            .Where(char.IsDigit)
            .ToArray());

    private static bool CpfValido(string cpf)
    {
        for (var digito = 9; digito <= 10; digito++)
        {
            var soma = 0;

            for (var i = 0; i < digito; i++)
            {
                soma +=
                    (cpf[i] - '0') *
                    (digito + 1 - i);
            }

            var resto = soma * 10 % 11;

            if (resto == 10)
            {
                resto = 0;
            }

            if (resto != cpf[digito] - '0')
            {
                return false;
            }
        }

        return true;
    }
}
