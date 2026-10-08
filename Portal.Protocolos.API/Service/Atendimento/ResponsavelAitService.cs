using Portal.Protocolos.API.Models.Atendimento.Ait;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Exceptions;
using System.Globalization;
using System.Text;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class ResponsavelAitService
{
    private readonly SitranApiClient _sitran;
    private readonly ProcessoCondutorRepository _processoCondutorRepository;

    public ResponsavelAitService(
        SitranApiClient sitran,
        ProcessoCondutorRepository processoCondutorRepository)
    {
        _sitran = sitran;
        _processoCondutorRepository = processoCondutorRepository;
    }

    public async Task<ResponsavelAitResponse> ObterAtualAsync(
        AutoInfracaoResponse auto,
        CancellationToken cancellationToken = default)
    {
        if (auto is null)
            throw new ArgumentNullException(nameof(auto));

        var proprietario = new ResponsavelAitResponse
        {
            Tipo = "PROPRIETARIO",
            Nome = auto.Proprietario?.Trim() ?? string.Empty,
            Documento = NormalizarDocumento(auto.DocumentoProprietario),
            CondutorReconhecido = false,
            ApresentacaoCondutorPendente = false,
            ApresentacaoCondutorIndeferida = false,
            DefesaBloqueada = false
        };

        var retornoCondutor = await _sitran.ObterCondutorAsync(
            auto.NumeroAuto,
            cancellationToken);

        // Não existe apresentação de condutor no SITRAN.
        // O responsável continua sendo o proprietário.
        if (retornoCondutor is null)
            return proprietario;

        var numeroProcesso = retornoCondutor.NumeroProcesso?.Trim() ?? string.Empty;
        var resultado = ClassificarResultadoCondutor(retornoCondutor);

        // Enquanto o DETRAN ainda não concluiu a análise da apresentação de
        // condutor, mantemos o proprietário como responsável atual do AIT.
        //
        // A existência do processo de Condutor é apenas uma INFORMAÇÃO para o
        // atendimento. Ela não bloqueia a abertura/finalização da Defesa.
        // A mudança de responsável somente ocorre se o DETRAN reconhecer o
        // condutor posteriormente.
        if (resultado == ResultadoCondutor.Pendente)
        {
            return new ResponsavelAitResponse
            {
                Tipo = "PROPRIETARIO",
                Nome = auto.Proprietario?.Trim() ?? string.Empty,
                Documento = NormalizarDocumento(auto.DocumentoProprietario),
                CondutorReconhecido = false,
                ApresentacaoCondutorPendente = true,
                ApresentacaoCondutorIndeferida = false,
                DefesaBloqueada = false,
                MotivoBloqueioDefesa = null,
                NumeroProcessoCondutor =
                    string.IsNullOrWhiteSpace(numeroProcesso)
                        ? null
                        : numeroProcesso,
                ResultadoCodigo = retornoCondutor.Resultado?.Codigo,
                ResultadoDescricao = retornoCondutor.Resultado?.Descricao,
                RetornoDetran = retornoCondutor.RetornoDetran
            };
        }

        // O DETRAN concluiu que a indicação não foi reconhecida.
        // A responsabilidade volta ao proprietário e a Defesa pode seguir
        // normalmente pela regra do proprietário.
        if (resultado == ResultadoCondutor.Indeferido)
        {
            proprietario.ApresentacaoCondutorIndeferida = true;
            proprietario.NumeroProcessoCondutor =
                string.IsNullOrWhiteSpace(numeroProcesso)
                    ? null
                    : numeroProcesso;
            proprietario.ResultadoCodigo = retornoCondutor.Resultado?.Codigo;
            proprietario.ResultadoDescricao = retornoCondutor.Resultado?.Descricao;
            proprietario.RetornoDetran = retornoCondutor.RetornoDetran;

            return proprietario;
        }

        // A partir daqui o DETRAN reconheceu o condutor.
        if (string.IsNullOrWhiteSpace(numeroProcesso))
        {
            throw new BusinessException(
                "O SITRAN informa condutor reconhecido para este AIT, mas não retornou o número do processo de apresentação.");
        }

        var condutorLocal = await _processoCondutorRepository
            .ObterVinculadoPorProcessoAsync(auto.NumeroAuto, numeroProcesso);

        if (condutorLocal is null)
        {
            throw new BusinessException(
                "O SITRAN informa condutor reconhecido para este AIT, mas o Portal não localizou o CPF vinculado ao processo " +
                numeroProcesso + ". A abertura deve ser bloqueada até a conferência.");
        }

        var documentoCondutor = NormalizarDocumento(condutorLocal.DocumentoCondutor);

        if (documentoCondutor.Length != 11)
        {
            throw new BusinessException(
                "O processo de condutor reconhecido possui documento inválido no Portal.");
        }

        return new ResponsavelAitResponse
        {
            Tipo = "CONDUTOR",
            Nome = retornoCondutor.Condutor?.Nome?.Trim()
                ?? condutorLocal.NomeCondutor?.Trim()
                ?? string.Empty,
            Documento = documentoCondutor,
            CondutorReconhecido = true,
            ApresentacaoCondutorPendente = false,
            ApresentacaoCondutorIndeferida = false,
            DefesaBloqueada = false,
            ProcessoCondutorId = condutorLocal.ProcessoCondutorId,
            ProcessoId = condutorLocal.ProcessoId,
            NumeroProcessoCondutor = numeroProcesso,
            ResultadoCodigo = retornoCondutor.Resultado?.Codigo,
            ResultadoDescricao = retornoCondutor.Resultado?.Descricao,
            RetornoDetran = retornoCondutor.RetornoDetran
        };
    }

    public void ValidarDefesaLiberada(ResponsavelAitResponse responsavel)
    {
        // Mantido por compatibilidade com os serviços que já chamam este método.
        // Apresentação de Condutor pendente não bloqueia mais a Defesa.
        // Bloqueios reais continuam sendo definidos pelas regras/elegibilidade
        // normais do serviço e por processos bloqueantes do próprio serviço.
        _ = responsavel;
    }

    public async Task<ResponsavelAitResponse> ValidarDefesaLiberadaAsync(
        AutoInfracaoResponse auto,
        CancellationToken cancellationToken = default)
    {
        var responsavel = await ObterAtualAsync(auto, cancellationToken);
        ValidarDefesaLiberada(responsavel);
        return responsavel;
    }

    public bool DocumentoCorresponde(
        string? documentoInformado,
        string? documentoResponsavel)
    {
        var informado = NormalizarDocumento(documentoInformado);
        var responsavel = NormalizarDocumento(documentoResponsavel);

        if (informado.Length != 11)
            return false;

        if (responsavel == informado)
            return true;

        if (responsavel.Length != 14)
            return false;

        return
            responsavel.StartsWith("000", StringComparison.Ordinal) &&
            responsavel[3..] == informado
            ||
            responsavel.EndsWith("000", StringComparison.Ordinal) &&
            responsavel[..11] == informado;
    }

    public async Task<ResponsavelAitResponse> ValidarDocumentoResponsavelAsync(
        AutoInfracaoResponse auto,
        string documento,
        CancellationToken cancellationToken = default)
    {
        var responsavel = await ObterAtualAsync(auto, cancellationToken);

        ValidarDefesaLiberada(responsavel);

        if (!DocumentoCorresponde(documento, responsavel.Documento))
        {
            var identificacao = responsavel.CondutorReconhecido
                ? "condutor reconhecido pelo DETRAN"
                : "proprietário do AIT";

            throw new BusinessException(
                $"O CPF informado não corresponde ao {identificacao}. " +
                "A abertura direta deve ser realizada pelo responsável atual do AIT.");
        }

        return responsavel;
    }

    private static ResultadoCondutor ClassificarResultadoCondutor(
        SitranCondutorResponse retorno)
    {
        var codigo = NormalizarTexto(retorno.Resultado?.Codigo);

        if (codigo is "I" or "INDEFERIDO" or "INDEFERIDA" or "REJEITADO" or "REJEITADA")
            return ResultadoCondutor.Indeferido;

        if (codigo is "D" or "DEFERIDO" or "DEFERIDA" or "RECONHECIDO" or "RECONHECIDA")
            return ResultadoCondutor.Reconhecido;

        var texto = NormalizarTexto(string.Join(
            " ",
            retorno.Resultado?.Descricao,
            retorno.RetornoDetran));

        if (texto.Contains("NAO RECONHEC", StringComparison.Ordinal) ||
            texto.Contains("INDEFER", StringComparison.Ordinal) ||
            texto.Contains("REJEIT", StringComparison.Ordinal) ||
            texto.Contains("RECUS", StringComparison.Ordinal))
        {
            return ResultadoCondutor.Indeferido;
        }

        if (texto.Contains("RECONHECID", StringComparison.Ordinal) ||
            texto.Contains("DEFERID", StringComparison.Ordinal))
        {
            return ResultadoCondutor.Reconhecido;
        }

        // Existe processo de apresentação no SITRAN, porém não há decisão final.
        return ResultadoCondutor.Pendente;
    }

    private static string NormalizarDocumento(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string NormalizarTexto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var normalizado = valor.Trim()
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();

        foreach (var caractere in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);
            if (categoria != UnicodeCategory.NonSpacingMark)
                sb.Append(caractere);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private enum ResultadoCondutor
    {
        Pendente,
        Reconhecido,
        Indeferido
    }
}
