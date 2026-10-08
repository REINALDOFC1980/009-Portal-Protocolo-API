using Microsoft.AspNetCore.Http;
using Portal.Protocolos.API.Models.Atendimento.Condutor;
using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class CondutorService
{
    private readonly ProcessoService _processoService;
    private readonly ProcessoRepository _processoRepository;
    private readonly ProcessoCondutorRepository _processoCondutorRepository;
    private readonly ProcessoMovimentacaoService _movimentacaoService;
    private readonly ProcessoAnexoService _processoAnexoService;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly DbSession _dbSession;

    private readonly SitranApiClient _sitranApiClient;

    public CondutorService(
     ProcessoService processoService,
     ProcessoRepository processoRepository,
     ProcessoCondutorRepository processoCondutorRepository,
     ProcessoMovimentacaoService movimentacaoService,
     ProcessoAnexoService processoAnexoService,
     UsuarioRepository usuarioRepository,
     DbSession dbSession,
     SitranApiClient sitranApiClient)
    {
        _processoService = processoService;
        _processoRepository = processoRepository;
        _processoCondutorRepository = processoCondutorRepository;
        _movimentacaoService = movimentacaoService;
        _processoAnexoService = processoAnexoService;
        _usuarioRepository = usuarioRepository;
        _dbSession = dbSession;
        _sitranApiClient = sitranApiClient;
    }

    public async Task<(long ProcessoId, string NumeroProcesso)> CriarAsync(
        int usuarioId,
        CondutorCreateRequest request,
        IReadOnlyList<IFormFile> arquivos,
        IReadOnlyList<string>? tiposDocumento)
    {
        //
        // VALIDAÇÕES
        // Tudo que pudermos validar antes de abrir a transação
        // deve ser validado aqui.
        //



        var elegibilidadeSitran =
    await _sitranApiClient
        .ObterElegibilidadeAsync(
            request.Auto.NumeroAuto);

        var contexto =
            new ContextoServico
            {
                Auto =
                    request.Auto,

                ElegibilidadeSitran =
                    elegibilidadeSitran
            };

        var policy =
            new CondutorPolicy();

        var resultado =
            policy.Avaliar(
                contexto);

        if (!resultado.Disponivel)
        {
            throw new BusinessException(
                resultado.Motivo ??
                "Não é possível abrir Apresentação de Condutor para este AIT.");
        }

        var documentoCondutor =
            SomenteNumeros(
                request.DocumentoCondutor);

        if (string.IsNullOrWhiteSpace(
                documentoCondutor))
        {
            throw new BusinessException(
                "Informe o CPF do condutor.");
        }

        if (documentoCondutor.Length != 11)
        {
            throw new BusinessException(
                "CPF do condutor inválido.");
        }

        var documentoProprietario =
            await _usuarioRepository
                .ObterDocumentoPorIdAsync(
                    usuarioId);

        documentoProprietario =
            SomenteNumeros(
                documentoProprietario);

        if (string.IsNullOrWhiteSpace(
                documentoProprietario))
        {
            throw new BusinessException(
                "Não foi possível identificar o documento do proprietário.");
        }

        if (documentoCondutor ==
            documentoProprietario)
        {
            throw new BusinessException(
                "O condutor indicado não pode ser o proprietário do veículo.");
        }

        if (arquivos == null ||
            arquivos.Count == 0)
        {
            throw new BusinessException(
                "É necessário anexar os documentos obrigatórios.");
        }



        using var connection =
            _dbSession.CreateConnection();

        using var transaction =
            connection.BeginTransaction();

        long processoId = 0;

        try
        {
            //
            // PROCESSO
            //

            var processo =
                await _processoService.CriarAsync(
                    usuarioId,
                    ServicoConstants.Condutor,
                    SituacaoProcessoConstants.AguardandoAceiteCondutor,
                    request.Auto,
                    connection,
                    transaction);

            processoId =
                processo.ProcessoId;

            //
            // PROCESSO CONDUTOR
            //

            var processoCondutor =
             new ProcessoCondutor
             {
                 ProcessoId =
                     processo.ProcessoId,

                 DocumentoCondutor =
                     documentoCondutor,

                 NomeCondutor =
                     string.Empty,

                 RegistroCnh =
                     string.Empty,

                 UfCnh =
                     string.Empty,

                 ModeloCnh =
                     string.Empty,

                 UsuarioCondutorId =
                     null,

                 Aceito =
                     null
             };

            await _processoCondutorRepository
                .InserirAsync(
                    processoCondutor,
                    connection,
                    transaction);

            //
            // MOVIMENTAÇÃO
            //

            await _movimentacaoService
                .RegistrarAsync(
                    processo.ProcessoId,
                    usuarioId,
                    SituacaoProcessoConstants.AguardandoAceiteCondutor,
                    "CONDUTOR_INDICADO",
                    "Condutor indicado para o processo. Aguardando aceite.",
                    connection,
                    transaction);

            //
            // ANEXOS
            //

            for (var i = 0;
                 i < arquivos.Count;
                 i++)
            {
                var arquivo =
                    arquivos[i];

                string? tipoDocumento =
                    null;

                if (tiposDocumento != null &&
                    i < tiposDocumento.Count)
                {
                    tipoDocumento =
                        tiposDocumento[i];
                }

                await _processoAnexoService
                    .InserirAsync(
                        processo.ProcessoId,
                        arquivo,
                        tipoDocumento,
                        connection,
                        transaction);
            }

            //
            // SOMENTE AGORA O PROCESSO EXISTE
            // DEFINITIVAMENTE.
            //

            transaction.Commit();

            return (
                processo.ProcessoId,
                processo.NumeroProcesso);
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

            //
            // A transação SQL não controla o filesystem.
            // Remove todos os PDFs que possam ter sido
            // gravados antes da falha.
            //

            if (processoId > 0)
            {
                _processoAnexoService
                    .RemoverPastaProcesso(
                        processoId);
            }

            throw;
        }
    }

    public async Task<List<CondutorPendenteResponse>>
        ListarPendentesAsync(
            string documento)
    {
        var cpf =
            SomenteNumeros(
                documento);

        return await _processoCondutorRepository
            .ListarProcessosPendentesAsync(
                cpf);
    }

    public async Task<(
        long ProcessoId,
        string NumeroProcesso,
        int SituacaoProcessoId)> AceitarAsync(
        long processoCondutorId,
        int usuarioCondutorId,
        string documento)
    {
        var processoCondutor =
            await _processoCondutorRepository
                .ObterPorIdAsync(
                    processoCondutorId);

        if (processoCondutor is null)
        {
            throw new BusinessException(
                "Processo de condutor não encontrado.");
        }

        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoCondutor.ProcessoId);

        if (processo is null)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        var cpf =
            SomenteNumeros(
                documento);

        if (string.IsNullOrWhiteSpace(
                cpf) ||
            processoCondutor.DocumentoCondutor !=
                cpf)
        {
            throw new BusinessException(
                "Este processo não pertence ao condutor autenticado.");
        }

        if (processoCondutor.Aceito.HasValue)
        {
            throw new BusinessException(
                "Este processo já foi respondido.");
        }

        if (
            processo.PrazoApresentacaoCondutor.HasValue &&
            processo.PrazoApresentacaoCondutor.Value.Date <
            DateTime.Today)
        {
            await _processoRepository
                .AtualizarSituacaoAsync(
                    processo.ProcessoId,
                    SituacaoProcessoConstants
                        .PrazoAceiteExpirado);

            await _movimentacaoService
                .RegistrarAsync(
                    processo.ProcessoId,
                    usuarioCondutorId,
                    SituacaoProcessoConstants
                        .PrazoAceiteExpirado,
                    "PRAZO_ACEITE_EXPIRADO",
                    "Prazo para aceite da apresentação de condutor encerrado.");

            throw new BusinessException(
                "O prazo para aceite da apresentação de condutor foi encerrado.");
        }

        //
        // USUÁRIO AUTENTICADO
        //
        // Nome e CNH pertencem ao próprio condutor.
        // O proprietário informou somente o CPF.
        //

        var usuarioCondutor =
            await _usuarioRepository
                .ObterPorDocumentoAsync(
                    cpf);

        if (usuarioCondutor is null)
        {
            throw new BusinessException(
                "Cadastro do condutor não encontrado.");
        }

        if (usuarioCondutor.UsuarioId !=
            usuarioCondutorId)
        {
            throw new BusinessException(
                "Os dados do usuário autenticado não correspondem ao condutor indicado.");
        }

        var nomeCondutor =
            usuarioCondutor.Nome?.Trim();

        if (string.IsNullOrWhiteSpace(
                nomeCondutor))
        {
            throw new BusinessException(
                "Nome do condutor não encontrado no cadastro.");
        }

        var registroCnh =
            usuarioCondutor.RegistroCnh
                ?.Trim();

        if (string.IsNullOrWhiteSpace(
                registroCnh))
        {
            throw new BusinessException(
                "Registro da CNH não informado no cadastro. Complete os dados da sua CNH antes de aceitar a apresentação.");
        }

        var ufCnh =
            usuarioCondutor.UfCnh
                ?.Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(
                ufCnh) ||
            ufCnh.Length != 2)
        {
            throw new BusinessException(
                "UF da CNH não informada ou inválida no cadastro.");
        }

        if (!usuarioCondutor.ModeloCnh.HasValue ||
            (usuarioCondutor.ModeloCnh.Value != 2 &&
             usuarioCondutor.ModeloCnh.Value != 3))
        {
            throw new BusinessException(
                "Modelo da CNH inválido. Informe Nacional ou Estrangeira no cadastro.");
        }

        var modeloCnh =
            usuarioCondutor.ModeloCnh.Value;

        //
        // PRIMEIRA ETAPA
        //
        // O aceite + snapshot dos dados do condutor
        // precisam estar definitivamente gravados antes
        // da integração externa.
        //

        using (var connection =
               _dbSession.CreateConnection())
        {
            using var transaction =
                connection.BeginTransaction();

            try
            {
                await _processoCondutorRepository
                    .AceitarComDadosAsync(
                        processoCondutorId,
                        usuarioCondutorId,
                        nomeCondutor,
                        registroCnh,
                        ufCnh,
                        modeloCnh,
                        connection,
                        transaction);

                await _processoRepository
                    .AtualizarSituacaoAsync(
                        processo.ProcessoId,
                        SituacaoProcessoConstants
                            .AguardandoRetornoExterno,
                        connection,
                        transaction);

                await _movimentacaoService
                    .RegistrarAsync(
                        processo.ProcessoId,
                        usuarioCondutorId,
                        SituacaoProcessoConstants
                            .AguardandoRetornoExterno,
                        "CONDUTOR_ACEITO",
                        "Condutor indicado aceitou a apresentação. Aguardando confirmação da integração.",
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

        //
        // O ACEITE JÁ ESTÁ PERSISTIDO.
        //
        // Se a integração falhar:
        // - o aceite continua válido;
        // - os dados usados ficam registrados no processo;
        // - o processo permanece na situação 6;
        // - o mesmo protocolo poderá ser reprocessado.
        //

        try
        {
            await _sitranApiClient
                .AbrirCondutorAsync(
                    processo.NumeroAIT,
                    processo.NumeroProcesso,
                    DateOnly.FromDateTime(
                        DateTime.Today),
                    nomeCondutor,
                    registroCnh,
                    ufCnh,
                    modeloCnh,
                    "PORTAL");
        }
        catch (SitranApiException)
        {
            return (
                processo.ProcessoId,
                processo.NumeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }
        catch (HttpRequestException)
        {
            return (
                processo.ProcessoId,
                processo.NumeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }
        catch (TaskCanceledException)
        {
            return (
                processo.ProcessoId,
                processo.NumeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoRetornoExterno
            );
        }

        //
        // INTEGRAÇÃO CONFIRMADA
        //

        await ConfirmarIntegracaoSitranAsync(
            processo.ProcessoId,
            usuarioCondutorId);

        return (
            processo.ProcessoId,
            processo.NumeroProcesso,
            SituacaoProcessoConstants
                .AguardandoTriagem
        );
    }

    public async Task<(long ProcessoId,string NumeroProcesso,int SituacaoProcessoId)> ReprocessarAsync(
     long processoCondutorId,
     int usuarioId,
     string documento)
    {
        var processoCondutor =
            await _processoCondutorRepository
                .ObterPorIdAsync(
                    processoCondutorId);

        if (processoCondutor is null)
        {
            throw new BusinessException(
                "Processo de condutor não encontrado.");
        }

        var cpf =
            SomenteNumeros(
                documento);

        if (string.IsNullOrWhiteSpace(cpf) ||
            processoCondutor.DocumentoCondutor != cpf)
        {
            throw new BusinessException(
                "Este processo não pertence ao condutor autenticado.");
        }

        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoCondutor.ProcessoId);

        if (processo is null)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        //
        // O reprocessamento só pode ocorrer depois
        // que o condutor efetivamente aceitou.
        //

        if (processoCondutor.Aceito != true)
        {
            throw new BusinessException(
                "A apresentação de condutor ainda não foi aceita.");
        }

        //
        // Somente processos aguardando integração
        // podem ser reprocessados.
        //

        if (processo.SituacaoProcessoId !=
            SituacaoProcessoConstants
                .AguardandoRetornoExterno)
        {
            throw new BusinessException(
                "Este processo não está aguardando integração.");
        }

        if (string.IsNullOrWhiteSpace(
                processoCondutor.NomeCondutor))
        {
            throw new BusinessException(
                "Nome do condutor não informado.");
        }

        if (string.IsNullOrWhiteSpace(
                processoCondutor.RegistroCnh))
        {
            throw new BusinessException(
                "Registro da CNH não informado.");
        }

        if (string.IsNullOrWhiteSpace(
                processoCondutor.UfCnh))
        {
            throw new BusinessException(
                "UF da CNH não informada.");
        }

        if (!processoCondutor.DataAceite.HasValue)
        {
            throw new BusinessException(
                "Data de aceite do condutor não encontrada.");
        }

        if (!byte.TryParse(
                processoCondutor.ModeloCnh,
                out var modeloCnh) ||
            (modeloCnh != 2 &&
             modeloCnh != 3))
        {
            throw new BusinessException(
                "Modelo da CNH inválido. Informe 2 para Nacional ou 3 para Estrangeira.");
        }

        //
        // PRIMEIRO CONSULTAMOS A INTEGRAÇÃO.
        //
        // Pode ter acontecido do POST anterior
        // ter sido processado e somente a resposta
        // não ter chegado ao Portal.
        //

        var condutorSitran =
            await _sitranApiClient
                .ObterCondutorAsync(
                    processo.NumeroAIT);

        if (condutorSitran is not null)
        {
            if (!string.Equals(
                    condutorSitran.NumeroProcesso
                        ?.Trim(),
                    processo.NumeroProcesso
                        ?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "Inconsistência de integração: já existe uma apresentação de condutor associada a outro número de processo.");
            }

            await ConfirmarIntegracaoSitranAsync(
                processo.ProcessoId,
                usuarioId);

            return (
                processo.ProcessoId,
                processo.NumeroProcesso,
                SituacaoProcessoConstants
                    .AguardandoTriagem
            );
        }

        //
        // NÃO EXISTE NA INTEGRAÇÃO.
        //
        // REENVIA O MESMO PROCESSO.
        //

        try
        {
            await _sitranApiClient
                .AbrirCondutorAsync(
                    processo.NumeroAIT,
                    processo.NumeroProcesso,
                    DateOnly.FromDateTime(
                        processoCondutor
                            .DataAceite
                            .Value),
                    processoCondutor
                        .NomeCondutor
                        .Trim(),
                    processoCondutor
                        .RegistroCnh
                        .Trim(),
                    processoCondutor
                        .UfCnh
                        .Trim()
                        .ToUpperInvariant(),
                    modeloCnh,
                    "PORTAL");
        }
        catch (SitranApiException ex)
            when (string.Equals(
                ex.Codigo,
                "PROCESSO_JA_EXISTE",
                StringComparison.OrdinalIgnoreCase))
        {
            //
            // Condição de corrida:
            //
            // GET não encontrou,
            // mas antes do POST o processo
            // foi registrado.
            //

            condutorSitran =
                await _sitranApiClient
                    .ObterCondutorAsync(
                        processo.NumeroAIT);

            if (condutorSitran is null)
            {
                throw;
            }

            if (!string.Equals(
                    condutorSitran.NumeroProcesso
                        ?.Trim(),
                    processo.NumeroProcesso
                        ?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "Inconsistência de integração: o processo existente possui outro número de protocolo.");
            }
        }

        //
        // O MESMO PROTOCOLO ESTÁ CONFIRMADO.
        //

        await ConfirmarIntegracaoSitranAsync(
            processo.ProcessoId,
            usuarioId);

        return (
            processo.ProcessoId,
            processo.NumeroProcesso,
            SituacaoProcessoConstants
                .AguardandoTriagem
        );
    }

    public async Task RecusarAsync(
        long processoCondutorId,
        int usuarioCondutorId,
        string documento,
        string? motivo)
    {
        var processoCondutor =
            await _processoCondutorRepository
                .ObterPorIdAsync(
                    processoCondutorId);

        if (processoCondutor is null)
        {
            throw new BusinessException(
                "Processo de condutor não encontrado.");
        }

        var processo =
            await _processoRepository
                .ObterPorIdAsync(
                    processoCondutor.ProcessoId);

        if (processo is null)
        {
            throw new BusinessException(
                "Processo não encontrado.");
        }

        var cpf =
            SomenteNumeros(
                documento);

        if (processoCondutor.DocumentoCondutor != cpf)
        {
            throw new BusinessException(
                "Este processo não pertence ao condutor autenticado.");
        }

        if (processoCondutor.Aceito.HasValue)
        {
            throw new BusinessException(
                "Este processo já foi respondido.");
        }

        if (
            processo.PrazoApresentacaoCondutor.HasValue &&
            processo.PrazoApresentacaoCondutor.Value.Date <
            DateTime.Today
        )
        {
            await _processoRepository
                .AtualizarSituacaoAsync(
                    processo.ProcessoId,
                    SituacaoProcessoConstants.PrazoAceiteExpirado);

            await _movimentacaoService
                .RegistrarAsync(
                    processo.ProcessoId,
                    usuarioCondutorId,
                    SituacaoProcessoConstants.PrazoAceiteExpirado,
                    "PRAZO_ACEITE_EXPIRADO",
                    "Prazo para resposta da apresentação de condutor encerrado.");

            throw new BusinessException(
                "O prazo para aceite da apresentação de condutor foi encerrado.");
        }

        await _processoCondutorRepository
            .RecusarAsync(
                processoCondutorId,
                usuarioCondutorId,
                motivo);

        await _processoRepository
            .AtualizarSituacaoAsync(
                processoCondutor.ProcessoId,
                SituacaoProcessoConstants.RecusadoCondutor);

        var descricao =
            string.IsNullOrWhiteSpace(
                motivo)
                ? "Condutor indicado recusou a apresentação."
                : $"Condutor indicado recusou a apresentação. Motivo: {motivo.Trim()}";

        await _movimentacaoService
            .RegistrarAsync(
                processoCondutor.ProcessoId,
                usuarioCondutorId,
                SituacaoProcessoConstants.RecusadoCondutor,
                "CONDUTOR_RECUSADO",
                descricao);
    }

    private async Task ConfirmarIntegracaoSitranAsync(
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
                    "Apresentação de condutor registrada com sucesso. Processo encaminhado para triagem.",
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
