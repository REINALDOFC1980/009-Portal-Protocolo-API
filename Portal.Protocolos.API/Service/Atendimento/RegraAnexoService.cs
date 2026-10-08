using Microsoft.Data.SqlClient;
using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class RegraAnexoService
{
    private readonly RegraAnexoRepository _repository;

    public RegraAnexoService(RegraAnexoRepository repository) => _repository = repository;

    public Task<IReadOnlyList<RegraAnexoResponse>> ListarAsync(
        short servicoId, string? origem, string? tipoSolicitante, bool incluirInativos)
    {
        ValidarFiltros(servicoId, origem, tipoSolicitante);
        return _repository.ListarAsync(servicoId, origem!.Trim().ToUpperInvariant(),
            NormalizarTipo(tipoSolicitante), incluirInativos);
    }

    public async Task<int> CriarAsync(RegraAnexoSalvarRequest request)
    {
        var dados = ValidarENormalizar(request);
        try
        {
            return await _repository.CriarAsync(dados);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new RegraAnexoDuplicadaException();
        }
    }

    public async Task<bool> AtualizarAsync(int id, RegraAnexoSalvarRequest request)
    {
        var dados = ValidarENormalizar(request);
        if (id <= 0) throw new BusinessException("Regra inválida.");

        try
        {
            return await _repository.AtualizarAsync(id, dados);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new RegraAnexoDuplicadaException();
        }
    }

    private static RegraAnexoSalvarRequest ValidarENormalizar(RegraAnexoSalvarRequest request)
    {
        if (request is null) throw new BusinessException("Informe os dados da regra.");
        ValidarFiltros(request.ServicoId, request.Origem, request.TipoSolicitante);
        if (string.IsNullOrWhiteSpace(request.CodigoDocumento) || request.CodigoDocumento.Trim().Length > 50)
            throw new BusinessException("Informe um código de documento de até 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.NomeDocumento) || request.NomeDocumento.Trim().Length > 150)
            throw new BusinessException("Informe um nome de documento de até 150 caracteres.");

        return new RegraAnexoSalvarRequest
        {
            ServicoId = request.ServicoId,
            Origem = request.Origem.Trim().ToUpperInvariant(),
            TipoSolicitante = NormalizarTipo(request.TipoSolicitante),
            CodigoDocumento = request.CodigoDocumento.Trim().ToUpperInvariant(),
            NomeDocumento = request.NomeDocumento.Trim(),
            Obrigatorio = request.Obrigatorio,
            Ativo = request.Ativo,
            Ordem = request.Ordem
        };
    }

    private static void ValidarFiltros(short servicoId, string? origem, string? tipo)
    {
        if (servicoId <= 0) throw new BusinessException("Informe o serviço.");
        if (origem?.Trim().ToUpperInvariant() is not ("SETAP" or "CORREIOS"))
            throw new BusinessException("Origem inválida.");
        if (NormalizarTipo(tipo) is not (null or "PROPRIETARIO" or "PROCURADOR" or "CONDUTOR"))
            throw new BusinessException("Tipo de solicitante inválido.");
    }

    private static string? NormalizarTipo(string? tipo) =>
        string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpperInvariant();
}

public sealed class RegraAnexoDuplicadaException : Exception
{
    public RegraAnexoDuplicadaException()
        : base("Já existe uma regra para este documento e contexto.") { }
}
