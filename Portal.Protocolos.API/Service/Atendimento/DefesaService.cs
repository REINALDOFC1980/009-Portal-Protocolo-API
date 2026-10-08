using Microsoft.AspNetCore.Http;
using Portal.Protocolos.API.Models.Atendimento.Defesa;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class DefesaService
{
    private readonly ProcessoService _processoService;
    private readonly ProcessoAnexoService _processoAnexoService;
    private readonly ProcessoRepository _processoRepository;
    private readonly ProcessoMovimentacaoService _movimentacaoService;
    private readonly AutoInfracaoService _autoInfracaoService;
    private readonly ResponsavelAitService _responsavelAitService;
    private readonly SitranApiClient _sitranApiClient;
    private readonly DbSession _dbSession;

    public DefesaService(
        ProcessoService processoService,
        ProcessoAnexoService processoAnexoService,
        ProcessoRepository processoRepository,
        ProcessoMovimentacaoService movimentacaoService,
        AutoInfracaoService autoInfracaoService,
        ResponsavelAitService responsavelAitService,
        SitranApiClient sitranApiClient,
        DbSession dbSession)
    {
        _processoService = processoService;
        _processoAnexoService = processoAnexoService;
        _processoRepository = processoRepository;
        _movimentacaoService = movimentacaoService;
        _autoInfracaoService = autoInfracaoService;
        _responsavelAitService = responsavelAitService;
        _sitranApiClient = sitranApiClient;
        _dbSession = dbSession;
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

        /*
         * O AIT é obtido novamente pelo backend.
         *
         * Não utilizamos os dados do AIT enviados
         * pelo navegador.
         */
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

        /*
         * REGRA DE RESPONSABILIDADE
         *
         * Se existe uma Apresentação de Condutor no SITRAN e o DETRAN ainda
         * não concluiu o resultado, a Defesa fica bloqueada para qualquer
         * pessoa. Depois do resultado, a responsabilidade volta ao proprietário
         * se a indicação for indeferida ou passa ao condutor se reconhecida.
         */
        var responsavelAtual =
            await _responsavelAitService
                .ObterAtualAsync(auto);

        _responsavelAitService
            .ValidarDefesaLiberada(responsavelAtual);

        /*
         * A elegibilidade oficial é consultada
         * diretamente no SITRAN.
         */
        var elegibilidade =
            await _sitranApiClient
                .ObterElegibilidadeAsync(
                    auto.NumeroAuto);

        if (elegibilidade is null)
        {
            throw new BusinessException(
                "Não foi possível consultar a elegibilidade do AIT.");
        }

        /*
         * A policy utiliza os dados atuais do AIT
         * juntamente com a elegibilidade do SITRAN.
         */
        var contexto =
            new ContextoServico
            {
                Auto = auto,
                ElegibilidadeSitran = elegibilidade
            };

        var policy =
            new DefesaPolicy();

        var resultado =
            policy.Avaliar(contexto);

        if (!resultado.Disponivel)
        {
            throw new BusinessException(
                resultado.Motivo ??
                "Não é possível abrir Defesa para este AIT.");
        }

        long processoId = 0;
        string numeroProcesso = string.Empty;

        /*
         * PRIMEIRA ETAPA
         *
         * O processo nasce primeiro no Portal.
         *
         * Situação:
         * 6 - Aguardando Retorno Externo
         */
        using (var connection =
               _dbSession.CreateConnection())
        {
            using var transaction =
                connection.BeginTransaction();

            try
            {
                var processo =
                    await _processoService.CriarAsync(
                        usuarioId,
                        ServicoConstants.Defesa,
                        SituacaoProcessoConstants.AguardandoRetornoExterno,
                        auto,
                        connection,
                        transaction);

                processoId =
                    processo.ProcessoId;

                numeroProcesso =
                    processo.NumeroProcesso;

                /*
                 * Anexos participam da mesma
                 * transação do processo.
                 */
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

                /*
                 * A partir daqui o processo existe
                 * oficialmente no Portal.
                 */
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
                    /*
                     * O rollback do banco não remove
                     * os arquivos físicos.
                     */
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

        /*
         * SEGUNDA ETAPA
         *
         * O processo já existe oficialmente
         * no Portal.
         *
         * Agora tentamos realizar a integração
         * com o SITRAN.
         *
         * Se houver falha de comunicação ou
         * falha retornada pelo SITRAN:
         *
         * - o processo permanece existente;
         * - mantém o mesmo protocolo;
         * - mantém os anexos;
         * - permanece na situação 6;
         * - poderá ser reprocessado posteriormente.
         */
        try
        {
            await _sitranApiClient
                .AbrirDefesaAsync(
                    auto.NumeroAuto,
                    numeroProcesso,
                    DateOnly.FromDateTime(
                        DateTime.Today));
        }
        catch (SitranApiException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants.AguardandoRetornoExterno
            );
        }
        catch (HttpRequestException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants.AguardandoRetornoExterno
            );
        }
        catch (TaskCanceledException)
        {
            return (
                processoId,
                numeroProcesso,
                SituacaoProcessoConstants.AguardandoRetornoExterno
            );
        }

        /*
         * TERCEIRA ETAPA
         *
         * O SITRAN confirmou.
         *
         * Agora alteramos para:
         * 3 - Aguardando Triagem
         */
        await ConfirmarIntegracaoAsync(
            processoId,
            usuarioId);

        return (
            processoId,
            numeroProcesso,
            SituacaoProcessoConstants.AguardandoTriagem
        );
    }


    public async Task<(long ProcessoId, string NumeroProcesso)> ReprocessarAsync(long processoId, int usuarioId)
    {
        /*
         * O reprocessamento nunca cria um novo
         * processo no Portal.
         *
         * Trabalhamos exclusivamente com o
         * ProcessoId já existente.
         */
        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoId);

        /*
         * Não revelamos dados de processos
         * pertencentes a outro usuário.
         */
        if (processo is null ||
            processo.UsuarioId != usuarioId)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        /*
         * Este serviço somente pode reprocessar
         * processos de Defesa.
         */
        if (processo.ServicoId !=
            ServicoConstants.Defesa)
        {
            throw new BusinessException(
                "O processo informado não é uma Defesa.");
        }

        /*
         * Reprocessamento somente é permitido
         * quando a integração externa ainda
         * está pendente.
         */
        if (processo.SituacaoProcessoId !=
            SituacaoProcessoConstants.AguardandoRetornoExterno)
        {
            throw new BusinessException(
                "Este processo não está aguardando retorno do SITRAN.");
        }

        /*
         * Primeiro verificamos se a Defesa já
         * existe no SITRAN.
         *
         * Isso resolve o cenário em que:
         *
         * 1. SITRAN gravou a Defesa;
         * 2. a resposta HTTP se perdeu;
         * 3. Portal permaneceu na situação 6.
         */
        var defesaSitran =
            await _sitranApiClient
                .ObterDefesaAsync(
                    processo.NumeroAIT);

        if (defesaSitran is not null)
        {
            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                defesaSitran.NumeroProcesso);

            /*
             * A Defesa já existe no SITRAN
             * utilizando exatamente o protocolo
             * originado pelo Portal.
             *
             * Portanto a integração ocorreu.
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
         * Não existe Defesa no SITRAN.
         *
         * Enviamos o MESMO protocolo já criado
         * no Portal.
         *
         * Não é gerado novo ProcessoId.
         * Não é gerado novo NumeroProcesso.
         *
         * Também preservamos a data original
         * de abertura do Portal.
         */
        try
        {
            await _sitranApiClient
                .AbrirDefesaAsync(
                    processo.NumeroAIT,
                    processo.NumeroProcesso,
                    DateOnly.FromDateTime(
                        processo.DataAbertura));
        }
        catch (SitranApiException ex)
            when (string.Equals(
                ex.Codigo,
                "PROCESSO_JA_EXISTE",
                StringComparison.OrdinalIgnoreCase))
        {
            /*
             * Existe uma possibilidade de corrida:
             *
             * GET informou que ainda não existia,
             * mas antes do POST a Defesa foi
             * registrada no SITRAN.
             *
             * Consultamos novamente e confirmamos
             * se é exatamente o nosso protocolo.
             */
            defesaSitran =
                await _sitranApiClient
                    .ObterDefesaAsync(
                        processo.NumeroAIT);

            if (defesaSitran is null)
            {
                throw;
            }

            ValidarMesmoProcesso(
                processo.NumeroProcesso,
                defesaSitran.NumeroProcesso);
        }

        /*
         * Neste ponto temos confirmação de que
         * o mesmo protocolo do Portal existe
         * no SITRAN.
         */
        await ConfirmarIntegracaoAsync(
            processo.ProcessoId,
            usuarioId);

        return (
            processo.ProcessoId,
            processo.NumeroProcesso
        );
    }

    public async Task<DefesaResponse> ObterAsync(long processoId, int usuarioId)
    {
        /*
         * O ProcessoId recebido é apenas a referência.
         *
         * AIT e protocolo são sempre obtidos
         * diretamente do banco do Portal.
         */
        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoId);

        /*
         * Não revelamos informações de processos
         * pertencentes a outro usuário.
         */
        if (processo is null ||
            processo.UsuarioId != usuarioId)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        /*
         * Este endpoint consulta exclusivamente
         * processos de Defesa.
         */
        if (processo.ServicoId !=
            ServicoConstants.Defesa)
        {
            throw new BusinessException(
                "O processo informado não é uma Defesa.");
        }

        /*
         * Consultamos o SITRAN utilizando o AIT
         * armazenado no próprio processo do Portal.
         */
        var defesaSitran =
            await _sitranApiClient
                .ObterDefesaAsync(
                    processo.NumeroAIT);

        if (defesaSitran is null)
        {
            throw new BusinessException(
                "A Defesa ainda não foi localizada no SITRAN.");
        }

        /*
         * O protocolo retornado pelo SITRAN precisa
         * obrigatoriamente ser o mesmo que nasceu
         * no Portal.
         */
        ValidarMesmoProcesso(
            processo.NumeroProcesso,
            defesaSitran.NumeroProcesso);

        return new DefesaResponse
        {
            ProcessoId =
                processo.ProcessoId,

            Protocolo =
                processo.NumeroProcesso,

            NumeroAit =
                defesaSitran.NumeroAit,

            DataAbertura =
                defesaSitran.DataAbertura,

            DataCadastro =
                defesaSitran.DataCadastro,

            UsuarioCadastro =
                defesaSitran.UsuarioCadastro,

            ResultadoCodigo =
                defesaSitran.Resultado?.Codigo,

            ResultadoDescricao =
                defesaSitran.Resultado?.Descricao,

            DataResultado =
                defesaSitran.DataResultado,

            DataBaixa =
                defesaSitran.DataBaixa,

            UsuarioBaixa =
                defesaSitran.UsuarioBaixa,

            DataPublicacao =
                defesaSitran.DataPublicacao,

            EfeitoSuspensivoAplicado =
                defesaSitran.EfeitoSuspensivoAplicado,

            CancelavelAte =
                defesaSitran.CancelavelAte
        };
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
                    "Defesa registrada no SITRAN e encaminhada para triagem.",
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
         * Esta situação viola a arquitetura:
         *
         * o Portal é a única origem dos processos
         * enviados ao SITRAN.
         *
         * Portanto não associamos automaticamente
         * um protocolo diferente.
         */
        throw new BusinessException(
            $"Inconsistência na integração com o SITRAN. " +
            $"O protocolo do Portal é '{numeroProcessoPortal}', " +
            $"mas o SITRAN possui '{numeroProcessoSitran}'.");
    }
}
