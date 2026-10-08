using Microsoft.AspNetCore.Http;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class JariService
{
    private readonly ProcessoService _processoService;
    private readonly ProcessoAnexoService _processoAnexoService;
    private readonly ProcessoRepository _processoRepository;
    private readonly ProcessoMovimentacaoService _movimentacaoService;
    private readonly AutoInfracaoService _autoInfracaoService;
    private readonly SitranApiClient _sitranApiClient;
    private readonly DbSession _dbSession;

    public JariService(
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

    public async Task<(
        long ProcessoId,
        string NumeroProcesso,
        int SituacaoProcessoId)> CriarAsync(
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

        //
        // AIT
        //

        var auto =
            await _autoInfracaoService
                .ObterPorNumeroAsync(
                    numeroAuto,
                    documentoUsuario);

        if (auto is null)
        {
            throw new BusinessException(
                "Auto de infração não encontrado.");
        }

        //
        // ELEGIBILIDADE OFICIAL SITRAN
        //

        var elegibilidade =
            await _sitranApiClient
                .ObterElegibilidadeAsync(
                    auto.NumeroAuto);

        if (elegibilidade is null)
        {
            throw new BusinessException(
                "Não foi possível consultar a elegibilidade do AIT.");
        }

        //
        // POLICY
        //

        var contexto =
            new ContextoServico
            {
                Auto =
                    auto,

                ElegibilidadeSitran =
                    elegibilidade
            };

        var policy =
            new JariPolicy();

        var resultado =
            policy.Avaliar(
                contexto);

        if (!resultado.Disponivel)
        {
            throw new BusinessException(
                resultado.Motivo ??
                "Não é possível abrir recurso JARI para este AIT.");
        }

        long processoId = 0;

        string numeroProcesso =
            string.Empty;

        //
        // 1ª ETAPA
        //
        // Primeiro persistimos o processo no Portal.
        //
        // Situação 6:
        // Aguardando Retorno Externo
        //

        using (var connection =
               _dbSession.CreateConnection())
        {
            using var transaction =
                connection.BeginTransaction();

            try
            {
                var processo =
                    await _processoService
                        .CriarAsync(
                            usuarioId,
                            ServicoConstants.Jari,
                            SituacaoProcessoConstants
                                .AguardandoRetornoExterno,
                            auto,
                            connection,
                            transaction);

                processoId =
                    processo.ProcessoId;

                numeroProcesso =
                    processo.NumeroProcesso;

                //
                // ANEXOS
                //

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
                    //
                    // O rollback SQL não remove
                    // os arquivos físicos.
                    //

                    if (processoId > 0)
                    {
                        _processoAnexoService
                            .RemoverPastaProcesso(
                                processoId);
                    }
                }

                throw;
            }
        }

        //
        // 2ª ETAPA
        //
        // O processo já existe oficialmente
        // no Portal.
        //
        // Agora tentamos realizar a integração
        // externa.
        //
        // Se houver falha:
        //
        // - o processo permanece existente;
        // - mantém o mesmo protocolo;
        // - mantém os anexos;
        // - permanece na situação 6;
        // - poderá ser reprocessado.
        //

        try
        {
            await _sitranApiClient
                .AbrirJariAsync(
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

        //
        // 3ª ETAPA
        //
        // Integração confirmada.
        //
        // Agora mudamos para:
        // 3 - Aguardando Triagem
        //

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
        //
        // Nunca criamos novo processo no Portal.
        //

        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoId);

        //
        // Segurança:
        // o processo precisa pertencer ao usuário.
        //

        if (processo is null ||
            processo.UsuarioId != usuarioId)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        //
        // Somente JARI.
        //

        if (processo.ServicoId !=
            ServicoConstants.Jari)
        {
            throw new BusinessException(
                "O processo informado não é um recurso JARI.");
        }

        //
        // Só reprocessamos integração pendente.
        //

        if (processo.SituacaoProcessoId !=
            SituacaoProcessoConstants
                .AguardandoRetornoExterno)
        {
            throw new BusinessException(
                "Este processo não está aguardando retorno do SITRAN.");
        }

        //
        // Antes de fazer POST novamente,
        // verificamos se já existe no SITRAN.
        //

        var jariSitran =
            await _sitranApiClient
                .ObterJariAsync(
                    processo.NumeroAIT);

        if (jariSitran is not null)
        {
            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                jariSitran.NumeroProcesso);

            await ConfirmarIntegracaoAsync(
                processo.ProcessoId,
                usuarioId);

            return (
                processo.ProcessoId,
                processo.NumeroProcesso
            );
        }

        //
        // Não existe no SITRAN.
        //
        // Enviamos novamente o MESMO protocolo,
        // preservando a data original de abertura.
        //

        try
        {
            await _sitranApiClient
                .AbrirJariAsync(
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
            //
            // Possível condição de corrida:
            //
            // GET retornou inexistente,
            // mas antes do POST o SITRAN
            // recebeu o processo.
            //

            jariSitran =
                await _sitranApiClient
                    .ObterJariAsync(
                        processo.NumeroAIT);

            if (jariSitran is null)
            {
                throw;
            }

            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                jariSitran.NumeroProcesso);
        }

        //
        // Neste ponto o mesmo protocolo
        // já está confirmado no SITRAN.
        //

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
                    SituacaoProcessoConstants
                        .AguardandoTriagem,
                    connection,
                    transaction);

            await _movimentacaoService
                .RegistrarAsync(
                    processoId,
                    usuarioId,
                    SituacaoProcessoConstants
                        .AguardandoTriagem,
                    "INTEGRACAO_SITRAN_CONFIRMADA",
                    "Recurso JARI encaminhado para triagem.",
                    connection,
                    transaction);

            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Não substitui a exceção original.
            }

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

        throw new BusinessException(
            $"Inconsistência na integração com o SITRAN. " +
            $"O protocolo do Portal é '{numeroProcessoPortal}', " +
            $"mas o SITRAN possui '{numeroProcessoSitran}'.");
    }
}
