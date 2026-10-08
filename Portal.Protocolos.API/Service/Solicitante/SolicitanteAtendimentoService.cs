using Portal.Protocolos.API.Models.Atendimento.Solicitante;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;
using System.Net.Mail;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class SolicitanteAtendimentoService
{
    private readonly SolicitanteAtendimentoRepository _repository;

    public SolicitanteAtendimentoService(SolicitanteAtendimentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConsultaSolicitanteResponse> ConsultarAsync(string? cpf)
    {
        var documento = SomenteNumeros(cpf);
        if (documento.Length != 11)
            throw new BusinessException("Informe um CPF com 11 dígitos.");

        var usuario = await _repository.ObterPorCpfAsync(documento);
        return new ConsultaSolicitanteResponse
        {
            Cadastrado = usuario is not null,
            Usuario = usuario
        };
    }

    public Task<SolicitanteAtendimentoResponse> CadastrarAsync(
        CadastrarSolicitanteRequest request) =>
        _repository.InserirAsync(ValidarENormalizar(request));

    public Task<SolicitanteAtendimentoResponse?> AtualizarAsync(
        string cpf, CadastrarSolicitanteRequest request)
    {
        var dados = ValidarENormalizar(request);
        if (SomenteNumeros(cpf) != dados.Cpf)
            throw new BusinessException("O CPF do cadastro não pode ser alterado.");
        return _repository.AtualizarAsync(dados);
    }

    private static CadastrarSolicitanteRequest ValidarENormalizar(
        CadastrarSolicitanteRequest request)
    {
        var cpf = SomenteNumeros(request.Cpf);
        var nome = request.Nome?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var celular = SomenteNumeros(request.Celular);
        var registro = string.IsNullOrWhiteSpace(request.RegistroCnh)
            ? null : request.RegistroCnh.Trim();
        var uf = string.IsNullOrWhiteSpace(request.UfCnh)
            ? null : request.UfCnh.Trim().ToUpperInvariant();
        var modelo = request.ModeloCnh;

        if (cpf.Length != 11 || cpf.Distinct().Count() == 1 || !CpfValido(cpf))
            throw new BusinessException("CPF inválido.");
        if (nome.Length is < 3 or > 150)
            throw new BusinessException("Informe o nome completo (3 a 150 caracteres).");
        if (email.Length > 200 || !MailAddress.TryCreate(email, out var endereco)
            || !string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Informe um e-mail válido.");
        if (celular.Length is < 10 or > 11)
            throw new BusinessException("Informe um celular válido com DDD.");

        var informouCnh = registro is not null || uf is not null || modelo.HasValue;
        if (informouCnh && (registro is null || registro.Length > 11 || uf is null
            || uf.Length != 2 || !uf.All(c => c is >= 'A' and <= 'Z') || !modelo.HasValue))
            throw new BusinessException("Ao informar a CNH, preencha registro (máximo 11 caracteres), UF e modelo.");
        if (modelo.HasValue && modelo.Value is not (2 or 3))
            throw new BusinessException("Modelo da CNH inválido.");

        return new CadastrarSolicitanteRequest
        {
            Cpf = cpf,
            Nome = nome,
            Email = email,
            Celular = celular,
            RegistroCnh = registro,
            UfCnh = uf,
            ModeloCnh = modelo
        };
    }

    private static string SomenteNumeros(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static bool CpfValido(string cpf)
    {
        for (var digito = 9; digito <= 10; digito++)
        {
            var soma = 0;
            for (var i = 0; i < digito; i++)
                soma += (cpf[i] - '0') * (digito + 1 - i);
            var resto = soma * 10 % 11;
            if (resto == 10) resto = 0;
            if (resto != cpf[digito] - '0') return false;
        }
        return true;
    }
}
