using Portal.Protocolos.API.Models.Atendimento.Condutor;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class CondutorPresencialService
{
    private readonly AgendamentoConsultaService _agendamentos;
    private readonly SitranApiClient _sitran;
    private readonly ServicoDisponivelService _servicosDisponiveis;
    private readonly ProcessoRepository _processos;
    private readonly ProcessoAtendimentoRepository _atendimentos;
    private readonly CondutorPresencialRepository _condutores;
    private readonly RegraAnexoRepository _regras;
    private readonly SolicitanteAtendimentoRepository _solicitantes;
    private readonly ProcessoAnexoService _anexos;
    private readonly ProcessoMovimentacaoService _movimentacoes;
    private readonly DbSession _db;

    public CondutorPresencialService(
        AgendamentoConsultaService agendamentos,
        SitranApiClient sitran,
        ServicoDisponivelService servicosDisponiveis,
        ProcessoRepository processos,
        ProcessoAtendimentoRepository atendimentos,
        CondutorPresencialRepository condutores,
        RegraAnexoRepository regras,
        SolicitanteAtendimentoRepository solicitantes,
        ProcessoAnexoService anexos,
        ProcessoMovimentacaoService movimentacoes,
        DbSession db)
    {
        _agendamentos = agendamentos;
        _sitran = sitran;
        _servicosDisponiveis = servicosDisponiveis;
        _processos = processos;
        _atendimentos = atendimentos;
        _condutores = condutores;
        _regras = regras;
        _solicitantes = solicitantes;
        _anexos = anexos;
        _movimentacoes = movimentacoes;
        _db = db;
    }

    public async Task<object> CriarAsync(
        int atendenteId,
        CondutorPresencialFormRequest form)
    {
        var origem = (form.Origem ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (origem is not ("CORREIOS" or "SETAP"))
            throw new BusinessException(
                "Origem inválida. Informe CORREIOS ou SETAP.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);

        if (origem == "CORREIOS")
        {
            if (!form.DataPostagem.HasValue)
                throw new BusinessException(
                    "Informe a data da postagem constante no documento dos Correios.");

            if (form.DataPostagem.Value > hoje)
                throw new BusinessException(
                    "A data da postagem não pode ser futura.");
        }

        var dataReferencia = origem == "CORREIOS"
            ? form.DataPostagem!.Value
            : hoje;

        DateTime? dataAberturaOficial = origem == "CORREIOS"
            ? dataReferencia.ToDateTime(TimeOnly.MinValue)
            : null;

        string numeroAuto;
        string cpfSolicitante;
        string? agendamentoId = null;
        string? tipoAgendado = null;

        if (origem == "SETAP")
        {
            if (string.IsNullOrWhiteSpace(form.AgendamentoId))
                throw new BusinessException(
                    "Informe o agendamento para atendimento SETAP.");

            var agendamento = await _agendamentos.ObterPorIdAsync(
                form.AgendamentoId.Trim());

            if (agendamento is null)
                throw new BusinessException("Agendamento não encontrado.");

            if (!agendamento.CodigoServico.Equals(
                    "CONDUTOR",
                    StringComparison.OrdinalIgnoreCase))
                throw new BusinessException(
                    "O agendamento não é de Apresentação de Condutor.");

            agendamentoId = agendamento.AgendamentoId;
            numeroAuto = agendamento.NumeroAit.Trim();
            cpfSolicitante = SomenteNumeros(agendamento.CpfSolicitante);
            tipoAgendado = agendamento.TipoSolicitante.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(form.NumeroAuto) &&
                !form.NumeroAuto.Trim().Equals(
                    numeroAuto,
                    StringComparison.OrdinalIgnoreCase))
                throw new BusinessException(
                    "O AIT informado não corresponde ao agendamento.");

            if (!string.IsNullOrWhiteSpace(form.DocumentoSolicitante) &&
                SomenteNumeros(form.DocumentoSolicitante) != cpfSolicitante)
                throw new BusinessException(
                    "O CPF informado não corresponde ao agendamento.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(form.AgendamentoId))
                throw new BusinessException(
                    "Correspondência dos Correios não possui agendamento.");

            numeroAuto = (form.NumeroAuto ?? string.Empty).Trim();
            cpfSolicitante = SomenteNumeros(form.DocumentoSolicitante);

            if (string.IsNullOrWhiteSpace(numeroAuto))
                throw new BusinessException(
                    "Informe o AIT da correspondência.");
        }

        if (cpfSolicitante.Length != 11)
            throw new BusinessException(
                "Informe o CPF do solicitante com 11 dígitos.");

        var solicitante = await _solicitantes.ObterPorCpfAsync(cpfSolicitante);

        if (solicitante is null)
            throw new BusinessException(
                "Cadastre o solicitante antes de abrir o processo.");

        var nomeSolicitante = solicitante.Nome.Trim();

        if (nomeSolicitante.Length is < 3 or > 150)
            throw new BusinessException(
                "Cadastro do solicitante possui nome inválido.");

        // Reconsulta o AIT e reavalia a elegibilidade no momento da abertura.
        var auto = await _sitran.ObterAitAsync(numeroAuto);

        if (auto is null ||
            !auto.NumeroAuto.Equals(
                numeroAuto,
                StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("AIT não encontrado no SITRAN.");

        var avaliacaoServicos = await _servicosDisponiveis.AvaliarAsync(
            new ContextoServico { Auto = auto });

        var avaliacaoCondutor = avaliacaoServicos.FirstOrDefault(
            s => s.ServicoId == ServicoConstants.Condutor);

        if (avaliacaoCondutor is null || !avaliacaoCondutor.Disponivel)
            throw new BusinessException(
                avaliacaoCondutor?.Motivo ??
                "Não é possível abrir Apresentação de Condutor para este AIT.");

        var solicitanteEhProprietario = DocumentoDoProprietarioCorresponde(
            cpfSolicitante,
            auto.DocumentoProprietario);

        // CONDUTOR: o atendimento só seleciona o CPF. Os demais dados vêm
        // exclusivamente do cadastro de Usuario e não são aceitos do Front.
        var documentoCondutor = SomenteNumeros(form.DocumentoCondutor);

        if (documentoCondutor.Length != 11)
            throw new BusinessException(
                "Informe o CPF do condutor com 11 dígitos.");

        if (DocumentoDoProprietarioCorresponde(
                documentoCondutor,
                auto.DocumentoProprietario))
            throw new BusinessException(
                "O condutor indicado não pode ser o proprietário do veículo.");

        // O tipo do solicitante NÃO é confiado ao Front.
        // A API determina automaticamente:
        // - solicitante = proprietário -> PROPRIETARIO
        // - solicitante = condutor      -> CONDUTOR
        // - diferente dos dois         -> PROCURADOR
        var solicitanteEhCondutor =
            !solicitanteEhProprietario &&
            cpfSolicitante == documentoCondutor;

        var tipoSolicitante = solicitanteEhProprietario
            ? "PROPRIETARIO"
            : solicitanteEhCondutor
                ? "CONDUTOR"
                : "PROCURADOR";

        var usuarioCondutor = await _solicitantes.ObterPorCpfAsync(
            documentoCondutor);

        if (usuarioCondutor is null)
            throw new BusinessException(
                "Condutor não cadastrado. Cadastre o usuário antes de continuar.");

        var nomeCondutor = (usuarioCondutor.Nome ?? string.Empty).Trim();
        var registroCnh = (usuarioCondutor.RegistroCnh ?? string.Empty).Trim();
        var ufCnh = (usuarioCondutor.UfCnh ?? string.Empty)
            .Trim()
            .ToUpperInvariant();
        var modeloCnh = usuarioCondutor.ModeloCnh;

        if (nomeCondutor.Length is < 3 or > 150)
            throw new BusinessException(
                "O cadastro do condutor possui nome inválido. Atualize na tela de Usuários.");

        if (registroCnh.Length is < 1 or > 11)
            throw new BusinessException(
                "O cadastro do condutor não possui um registro de CNH válido (máximo 11 caracteres). Atualize na tela de Usuários.");

        if (ufCnh.Length != 2 ||
            !ufCnh.All(c => c is >= 'A' and <= 'Z'))
            throw new BusinessException(
                "O cadastro do condutor não possui uma UF de CNH válida. Atualize na tela de Usuários.");

        if (!modeloCnh.HasValue || modeloCnh.Value is not (2 or 3))
            throw new BusinessException(
                "O cadastro do condutor não possui um modelo de CNH válido. Atualize na tela de Usuários.");

        if (form.Arquivos is null ||
            form.TiposDocumento is null ||
            form.Arquivos.Count != form.TiposDocumento.Count ||
            form.TiposDocumento.Any(string.IsNullOrWhiteSpace))
            throw new BusinessException(
                "Os anexos enviados precisam possuir um tipo de documento correspondente.");

        var regrasAtivas = await _regras.ListarAsync(
            ServicoConstants.Condutor,
            origem,
            tipoSolicitante,
            incluirInativos: false);

        var obrigatorios = regrasAtivas
            .Where(r => r.Obrigatorio)
            .GroupBy(
                r => r.CodigoDocumento.Trim().ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var enviados = form.TiposDocumento
            .Where(tipoDocumento => !string.IsNullOrWhiteSpace(tipoDocumento))
            .Select(tipoDocumento => tipoDocumento.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var naoConferidos = (form.DocumentosNaoConferidos ?? new List<string>())
            .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
            .Select(codigo => codigo.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var faltantes = obrigatorios
            .Where(regra =>
            {
                var codigo = regra.CodigoDocumento.Trim().ToUpperInvariant();
                return !enviados.Contains(codigo) ||
                       naoConferidos.Contains(codigo);
            })
            .GroupBy(
                r => r.CodigoDocumento.Trim().ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var faltaDocumentacao = faltantes.Count > 0;

        if (faltaDocumentacao && !form.PermitirFaltaDocumentacao)
            throw new BusinessException(
                "Existem documentos obrigatórios não conferidos: " +
                string.Join(", ", faltantes.Select(r => r.NomeDocumento)) +
                ". Confirme a continuidade com restrição por falta de documentação.");

        var documentosFaltantesCodigos = faltaDocumentacao
            ? Limitar1000(
                string.Join(", ", faltantes
                    .Select(r => r.CodigoDocumento.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)))
            : null;

        var documentosFaltantesNomes = faltantes
            .Select(r => r.NomeDocumento.Trim())
            .Where(nome => !string.IsNullOrWhiteSpace(nome))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        byte? restricao = form.Estrangeiro ? (byte)3 : null;

        long processoId = 0;
        long processoCondutorId = 0;
        string protocolo = string.Empty;

        // No atendimento interno não existe AGUARDANDO_ACEITE_CONDUTOR.
        // O aceite é registrado automaticamente antes do envio ao sistema externo.
        var situacao = SituacaoProcessoConstants.AguardandoRetornoExterno;

        using (var connection = _db.CreateConnection())
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                if (agendamentoId is not null &&
                    await _atendimentos.AgendamentoJaAtendidoAsync(
                        agendamentoId,
                        connection,
                        transaction))
                    throw new BusinessException(
                        "Este agendamento já foi atendido.");

                if (await _processos.ExisteProcessoBloqueanteAsync(
                        auto.NumeroAuto,
                        ServicoConstants.Condutor,
                        connection,
                        transaction))
                    throw new BusinessException(
                        "Já existe uma Apresentação de Condutor ativa para este AIT.");

                protocolo = await _processos.GerarNumeroProcessoAsync(
                    connection,
                    transaction);

                processoId = await _atendimentos.InserirProcessoPresencialAsync(
                    protocolo,
                    ServicoConstants.Condutor,
                    situacao,
                    auto,
                    dataAberturaOficial,
                    connection,
                    transaction);

                await _atendimentos.InserirAtendimentoAsync(
                    processoId,
                    atendenteId,
                    agendamentoId,
                    origem,
                    tipoSolicitante,
                    nomeSolicitante,
                    cpfSolicitante,
                    restricao,
                    form.DataPostagem,
                    faltaDocumentacao,
                    documentosFaltantesCodigos,
                    connection,
                    transaction);

                processoCondutorId = await _condutores.InserirAceitoAsync(
                    processoId,
                    usuarioCondutor.UsuarioId,
                    documentoCondutor,
                    nomeCondutor,
                    registroCnh,
                    ufCnh,
                    modeloCnh.Value.ToString(),
                    connection,
                    transaction);

                for (var i = 0; i < form.Arquivos.Count; i++)
                {
                    await _anexos.InserirAsync(
                        processoId,
                        protocolo,
                        form.Arquivos[i],
                        form.TiposDocumento[i],
                        connection,
                        transaction,
                        permitirImagens: true);
                }

                await _movimentacoes.RegistrarAsync(
                    processoId,
                    atendenteId,
                    situacao,
                    "PROCESSO_ABERTO",
                    $"Processo {protocolo} de Apresentação de Condutor aberto no atendimento. Origem: {origem}.",
                    connection,
                    transaction);

                await _movimentacoes.RegistrarAsync(
                    processoId,
                    atendenteId,
                    situacao,
                    "CONDUTOR_ACEITO_PRESENCIAL",
                    "Condutor confirmado no atendimento. Aceite registrado automaticamente e processo aguardando confirmação da integração externa.",
                    connection,
                    transaction);

                if (faltaDocumentacao)
                {
                    await _movimentacoes.RegistrarAsync(
                        processoId,
                        atendenteId,
                        situacao,
                        "RESTRICAO_FALTA_DOCUMENTACAO",
                        Limitar500(
                            "Processo aberto com restrição por falta de documentação. " +
                            "Documentos não conferidos: " +
                            string.Join(", ", documentosFaltantesNomes) + "."),
                        connection,
                        transaction);
                }

                transaction.Commit();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (
                ex.Number == 2601 &&
                ex.Message.Contains(
                    "UX_ProcessoAtendimento_AgendamentoId",
                    StringComparison.OrdinalIgnoreCase))
            {
                transaction.Rollback();

                if (processoId > 0)
                    _anexos.RemoverPastaProcesso(protocolo);

                throw new BusinessException(
                    "Este agendamento já foi atendido.");
            }
            catch
            {
                transaction.Rollback();

                if (processoId > 0)
                    _anexos.RemoverPastaProcesso(protocolo);

                throw;
            }
        }

        string? mensagemIntegracao = null;
        var integrado = false;
        var rejeitadoExterno = false;

        try
        {
            await _sitran.AbrirCondutorAsync(
                auto.NumeroAuto,
                protocolo,
                dataReferencia,
                nomeCondutor,
                registroCnh,
                ufCnh,
                modeloCnh.Value,
                "PORTAL");

            integrado = true;
        }
        catch (SitranApiException ex)
        {
            mensagemIntegracao = Limitar500(
                "O sistema externo não confirmou a apresentação de condutor: " +
                ex.Message);

            rejeitadoExterno = EhRejeicaoDoSistemaExterno(ex);
        }
        catch (HttpRequestException)
        {
            mensagemIntegracao =
                "Não foi possível comunicar com o sistema externo. Integração pendente.";
        }
        catch (TaskCanceledException)
        {
            mensagemIntegracao =
                "O sistema externo não respondeu no prazo. Integração pendente.";
        }

        if (integrado)
        {
            using var connection = _db.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                await _processos.AtualizarSituacaoAsync(
                    processoId,
                    SituacaoProcessoConstants.AguardandoTriagem,
                    connection,
                    transaction);

                await _movimentacoes.RegistrarAsync(
                    processoId,
                    atendenteId,
                    SituacaoProcessoConstants.AguardandoTriagem,
                    "INTEGRACAO_CONDUTOR_CONFIRMADA",
                    faltaDocumentacao
                        ? "Apresentação de condutor encaminhada ao sistema externo com aceite presencial e restrição por falta de documentação."
                        : "Apresentação de condutor encaminhada ao sistema externo com aceite presencial.",
                    connection,
                    transaction);

                transaction.Commit();
                situacao = SituacaoProcessoConstants.AguardandoTriagem;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        else if (rejeitadoExterno &&
                 !string.IsNullOrWhiteSpace(mensagemIntegracao))
        {
            using var connection = _db.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                await _processos.AtualizarSituacaoEMotivoRejeicaoAsync(
                    processoId,
                    SituacaoProcessoConstants.RejeitadoSitran,
                    mensagemIntegracao,
                    connection,
                    transaction);

                await _movimentacoes.RegistrarAsync(
                    processoId,
                    atendenteId,
                    SituacaoProcessoConstants.RejeitadoSitran,
                    "INTEGRACAO_CONDUTOR_REJEITADA",
                    mensagemIntegracao,
                    connection,
                    transaction);

                transaction.Commit();
                situacao = SituacaoProcessoConstants.RejeitadoSitran;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        return new
        {
            processoId,
            processoCondutorId,
            protocolo,
            situacaoProcessoId = situacao,
            tipoRestricaoId = restricao,
            faltaDocumentacao,
            documentosFaltantes = documentosFaltantesNomes,
            integracaoPendente =
                situacao == SituacaoProcessoConstants.AguardandoRetornoExterno,
            integracaoRejeitada =
                situacao == SituacaoProcessoConstants.RejeitadoSitran,
            mensagemIntegracao
        };
    }

    private static string SomenteNumeros(string? valor) =>
        new((valor ?? string.Empty)
            .Where(char.IsDigit)
            .ToArray());

    private static bool EhRejeicaoDoSistemaExterno(SitranApiException ex)
    {
        var status = (int)ex.StatusCode;

        return status >= 400 &&
               status < 500 &&
               status is not 401 and not 403 and not 408 and not 429;
    }

    private static string Limitar500(string valor)
    {
        var texto = valor.Trim();
        return texto.Length <= 500 ? texto : texto[..500];
    }

    private static string Limitar1000(string valor)
    {
        var texto = valor.Trim();
        return texto.Length <= 1000 ? texto : texto[..1000];
    }

    private static bool DocumentoDoProprietarioCorresponde(
        string cpf,
        string? documento)
    {
        var numeros = SomenteNumeros(documento);

        if (numeros == cpf)
            return true;

        if (numeros.Length != 14 || cpf.Length != 11)
            return false;

        return numeros.StartsWith("000", StringComparison.Ordinal) &&
               numeros[3..] == cpf
               || numeros.EndsWith("000", StringComparison.Ordinal) &&
               numeros[..11] == cpf;
    }
}
