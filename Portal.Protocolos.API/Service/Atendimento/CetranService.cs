using Microsoft.AspNetCore.Http;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class CetranService
{
    private readonly ProcessoService _processoService;
    private readonly ProcessoAnexoService _processoAnexoService;
    private readonly ProcessoRepository _processoRepository;
    private readonly ProcessoMovimentacaoService _movimentacaoService;
    private readonly AutoInfracaoService _autoInfracaoService;
    private readonly SitranApiClient _sitranApiClient;
    private readonly DbSession _dbSession;

    public CetranService(
        ProcessoService processoService,
        ProcessoAnexoService processoAnexoService,
        ProcessoRepository processoRepository,
        ProcessoMovimentacaoService movimentacaoService,
        AutoInfracaoService autoInfracaoService,
        SitranApiClient sitranApiClient,
        DbSession dbSession)
    {
        _processoService =
            processoService;

        _processoAnexoService =
            processoAnexoService;

        _processoRepository =
            processoRepository;

        _movimentacaoService =
            movimentacaoService;

        _autoInfracaoService =
            autoInfracaoService;

        _sitranApiClient =
            sitranApiClient;

        _dbSession =
            dbSession;
    }

    public async Task<(long ProcessoId,string NumeroProcesso,int SituacaoProcessoId)> CriarAsync(
        int usuarioId,
        string documentoUsuario,
        string numeroAuto,
        IReadOnlyList<IFormFile> arquivos,
        IReadOnlyList<string>? tiposDocumento)
    {
        if (string.IsNullOrWhiteSpace(numeroAuto))
        {
            throw new BusinessException(
                "O auto de infração não foi informado.");
        }

        if (string.IsNullOrWhiteSpace(documentoUsuario))
        {
            throw new BusinessException(
                "Não foi possível identificar o usuário.");
        }

        if (arquivos == null ||
            arquivos.Count == 0)
        {
            throw new BusinessException(
                "É necessário anexar os documentos obrigatórios.");
        }

        /*
         * Busca o AIT utilizando o documento
         * do usuário autenticado.
         */
        var auto =
            await _autoInfracaoService
                .ObterPorNumeroAsync(
                    numeroAuto,
                    documentoUsuario);

        if (auto == null)
        {
            throw new BusinessException(
                "Auto de infração não encontrado.");
        }

        /*
         * A elegibilidade para CETRAN é definida
         * pelo SITRAN.
         */
        var elegibilidade =
            await _sitranApiClient
                .ObterElegibilidadeAsync(
                    auto.NumeroAuto);

        if (elegibilidade == null)
        {
            throw new BusinessException(
                "Não foi possível consultar a elegibilidade do AIT no SITRAN.");
        }

        var contexto =
            new ContextoServico
            {
                Auto =
                    auto,

                ElegibilidadeSitran =
                    elegibilidade
            };

        /*
         * A regra específica do CETRAN continua
         * centralizada na Policy.
         */
        var policy =
            new CetranPolicy();

        var resultado =
            policy.Avaliar(
                contexto);

        if (!resultado.Disponivel)
        {
            throw new BusinessException(
                resultado.Motivo ??
                "Não é possível abrir recurso CETRAN para este AIT.");
        }

        /*
         * PRIMEIRA ETAPA
         *
         * Criamos primeiro o processo no Portal.
         *
         * Situação:
         * 6 - Aguardando Retorno Externo
         *
         * O processo e seus anexos precisam existir
         * antes de qualquer chamada ao SITRAN.
         */
        using var connection =
            _dbSession.CreateConnection();

        using var transaction =
            connection.BeginTransaction();

        long processoId = 0;
        string numeroProcesso = string.Empty;

        try
        {
            var processo =
                await _processoService.CriarAsync(
                    usuarioId,
                    ServicoConstants.Cetran,
                    SituacaoProcessoConstants.AguardandoRetornoExterno,
                    auto,
                    connection,
                    transaction);

            processoId =
                processo.ProcessoId;

            numeroProcesso =
                processo.NumeroProcesso;

            for (var i = 0;
                 i < arquivos.Count;
                 i++)
            {
                var tipoDocumento =
                    tiposDocumento != null &&
                    i < tiposDocumento.Count
                        ? tiposDocumento[i]
                        : null;

                await _processoAnexoService
                    .InserirAsync(
                        processo.ProcessoId,
                        arquivos[i],
                        tipoDocumento,
                        connection,
                        transaction);
            }

            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            finally
            {
                if (processoId > 0)
                {
                    _processoAnexoService
                        .RemoverPastaProcesso(
                            processoId);
                }
            }

            throw;
        }

        /*
         * SEGUNDA ETAPA
         *
         * Somente depois do commit local enviamos
         * o recurso CETRAN ao SITRAN.
         *
         * Se esta chamada falhar:
         *
         * - o processo continua existindo;
         * - permanece na situação 6;
         * - os anexos permanecem;
         * - poderá ser reprocessado.
         */
        try
        {
            await _sitranApiClient
                .AbrirCetranAsync(
                    auto.NumeroAuto,
                    numeroProcesso,
                    DateOnly.FromDateTime(
                        DateTime.Today),
                    "PORTAL");
        }
        catch (SitranApiException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }
        catch (HttpRequestException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }
        catch (TaskCanceledException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }

        await ConfirmarIntegracaoAsync(
            processoId,
            usuarioId);

        return (
            processoId,
            numeroProcesso,
            SituacaoProcessoConstants
                .AguardandoTriagem
        );
    }

    public async Task<(long ProcessoId, string NumeroProcesso)>
        ReprocessarAsync(
            long processoId,
            int usuarioId)
    {
        /*
         * O reprocessamento nunca cria um
         * novo processo no Portal.
         */
        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoId);

        /*
         * Não revelamos dados pertencentes
         * a outro usuário.
         */
        if (processo is null ||
            processo.UsuarioId != usuarioId)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        /*
         * Este serviço somente reprocessa
         * recursos CETRAN.
         */
        if (processo.ServicoId !=
            ServicoConstants.Cetran)
        {
            throw new BusinessException(
                "O processo informado não é um recurso CETRAN.");
        }

        /*
         * Somente processos aguardando
         * retorno externo podem ser reprocessados.
         */
        if (processo.SituacaoProcessoId !=
            SituacaoProcessoConstants.AguardandoRetornoExterno)
        {
            throw new BusinessException(
                "Este processo não está aguardando retorno do SITRAN.");
        }

        /*
         * Primeiro consultamos o SITRAN.
         *
         * Isso protege contra o cenário:
         *
         * 1. SITRAN gravou o processo;
         * 2. a resposta HTTP não chegou ao Portal;
         * 3. Portal permaneceu na situação 6.
         */
        var cetranSitran =
            await _sitranApiClient
                .ObterCetranAsync(
                    processo.NumeroAIT);

        if (cetranSitran is not null)
        {
            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                cetranSitran.NumeroProcesso);

            /*
             * Já existe no SITRAN e possui
             * exatamente o mesmo protocolo.
             */
            await ConfirmarIntegracaoAsync(
                processo.ProcessoId,
                usuarioId);

            return (
                processo.ProcessoId,
                processo.NumeroProcesso
            );
        }

        /*
         * Não existe CETRAN no SITRAN.
         *
         * Enviamos exatamente o processo
         * originalmente criado pelo Portal.
         *
         * ProcessoId, protocolo e data de
         * abertura são preservados.
         */
        try
        {
            await _sitranApiClient
                .AbrirCetranAsync(
                    processo.NumeroAIT,
                    processo.NumeroProcesso,
                    DateOnly.FromDateTime(
                        processo.DataAbertura),
                    "PORTAL");
        }
        catch (SitranApiException ex)
            when (string.Equals(
                ex.Codigo,
                "PROCESSO_JA_EXISTE",
                StringComparison.OrdinalIgnoreCase))
        {
            /*
             * Proteção contra condição de corrida.
             *
             * O GET poderia não encontrar o processo
             * e, antes do POST, ele ser registrado.
             *
             * Consultamos novamente.
             */
            cetranSitran =
                await _sitranApiClient
                    .ObterCetranAsync(
                        processo.NumeroAIT);

            if (cetranSitran is null)
            {
                throw;
            }

            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                cetranSitran.NumeroProcesso);
        }

        /*
         * Neste ponto sabemos que o mesmo
         * protocolo do Portal existe no SITRAN.
         */
        await ConfirmarIntegracaoAsync(
            processo.ProcessoId,
            usuarioId);

        return (
            processo.ProcessoId,
            processo.NumeroProcesso
        );
    }

    private async Task ConfirmarIntegracaoAsync(
        long processoId,
        int usuarioId)
    {
        using var connection =
            _dbSession.CreateConnection();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            await _processoRepository
                .AtualizarSituacaoAsync(
                    processoId,
                    SituacaoProcessoConstants.AguardandoTriagem,
                    connection,
                    transaction);

            await _movimentacaoService
                .RegistrarAsync(
                    processoId,
                    usuarioId,
                    SituacaoProcessoConstants.AguardandoTriagem,
                    "INTEGRACAO_SITRAN_CONFIRMADA",
                    "Recurso CETRAN encaminhado para triagem.",
                    connection,
                    transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
    }

    private static void ValidarMesmoProcesso(
        string numeroProcessoPortal,
        string numeroProcessoSitran)
    {
        if (string.Equals(
            numeroProcessoPortal?.Trim(),
            numeroProcessoSitran?.Trim(),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        /*
         * O Portal é a origem do processo.
         *
         * Se o SITRAN possuir outro protocolo
         * para o mesmo AIT, não associamos
         * automaticamente os processos.
         */
        throw new BusinessException(
            $"Inconsistência na integração com o SITRAN. " +
            $"O protocolo do Portal é '{numeroProcessoPortal}', " +
            $"mas o SITRAN possui '{numeroProcessoSitran}'.");
    }
}
