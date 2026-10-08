using Portal.Protocolos.API.Models.Atendimento.Defesa;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class DefesaPresencialService
{
    private readonly AgendamentoConsultaService _agendamentos;
    private readonly SitranApiClient _sitran;
    private readonly ResponsavelAitService _responsaveis;
    private readonly ProcessoRepository _processos;
    private readonly ProcessoAtendimentoRepository _atendimentos;
    private readonly RegraAnexoRepository _regras;
    private readonly SolicitanteAtendimentoRepository _solicitantes;
    private readonly ProcessoAnexoService _anexos;
    private readonly ProcessoMovimentacaoService _movimentacoes;
    private readonly DbSession _db;

    public DefesaPresencialService(
        AgendamentoConsultaService agendamentos,
        SitranApiClient sitran,
        ResponsavelAitService responsaveis,
        ProcessoRepository processos,
        ProcessoAtendimentoRepository atendimentos,
        RegraAnexoRepository regras,
        SolicitanteAtendimentoRepository solicitantes,
        ProcessoAnexoService anexos,
        ProcessoMovimentacaoService movimentacoes,
        DbSession db)
    {
        _agendamentos = agendamentos;
        _sitran = sitran;
        _responsaveis = responsaveis;
        _processos = processos;
        _atendimentos = atendimentos;
        _regras = regras;
        _solicitantes = solicitantes;
        _anexos = anexos;
        _movimentacoes = movimentacoes;
        _db = db;
    }

    public async Task<object> CriarAsync(
        int atendenteId,
        DefesaPresencialFormRequest form)
    {
        var origem = (form.Origem ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (origem is not ("CORREIOS" or "SETAP"))
        {
            throw new BusinessException(
                "Origem inválida. Informe CORREIOS ou SETAP.");
        }

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
        string cpf;
        string? agendamentoId = null;
        string? tipoAgendado = null;

        if (origem == "SETAP")
        {
            if (string.IsNullOrWhiteSpace(form.AgendamentoId))
            {
                throw new BusinessException(
                    "Informe o agendamento para atendimento SETAP.");
            }

            var agendamento = await _agendamentos.ObterPorIdAsync(
                form.AgendamentoId.Trim());

            if (agendamento is null)
            {
                throw new BusinessException(
                    "Agendamento não encontrado.");
            }

            if (!agendamento.CodigoServico.Equals(
                    "DEFESA",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "O agendamento não é de Defesa.");
            }

            agendamentoId = agendamento.AgendamentoId;
            numeroAuto = agendamento.NumeroAit.Trim();
            cpf = SomenteNumeros(agendamento.CpfSolicitante);
            tipoAgendado = agendamento.TipoSolicitante
                .Trim()
                .ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(form.NumeroAuto) &&
                !form.NumeroAuto.Trim().Equals(
                    numeroAuto,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "O AIT informado não corresponde ao agendamento.");
            }

            if (!string.IsNullOrWhiteSpace(form.DocumentoSolicitante) &&
                SomenteNumeros(form.DocumentoSolicitante) != cpf)
            {
                throw new BusinessException(
                    "O CPF informado não corresponde ao agendamento.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(form.AgendamentoId))
            {
                throw new BusinessException(
                    "Correspondência dos Correios não possui agendamento.");
            }

            numeroAuto = (form.NumeroAuto ?? string.Empty).Trim();
            cpf = SomenteNumeros(form.DocumentoSolicitante);

            if (string.IsNullOrWhiteSpace(numeroAuto))
            {
                throw new BusinessException(
                    "Informe o AIT da correspondência.");
            }
        }

        if (cpf.Length != 11)
        {
            throw new BusinessException(
                "Informe o CPF do solicitante com 11 dígitos.");
        }

        var solicitante = await _solicitantes.ObterPorCpfAsync(cpf);

        if (solicitante is null)
        {
            throw new BusinessException(
                "Cadastre o solicitante antes de abrir o processo.");
        }

        // O nome do solicitante sempre vem do cadastro confirmado no atendimento.
        // Mesmo quando o CPF corresponde ao proprietário do AIT, não substituímos
        // esse nome pelo nome devolvido pelo SITRAN.
        var nome = solicitante.Nome.Trim();

        if (nome.Length is < 3 or > 150)
        {
            throw new BusinessException(
                "Informe o nome do solicitante (3 a 150 caracteres).");
        }

        // Reconsulta o AIT no momento da abertura. O cadastro manual dos Correios
        // nunca substitui o proprietário/placa devolvidos pelo SITRAN.
        var auto = await _sitran.ObterAitAsync(numeroAuto);

        if (auto is null ||
            !auto.NumeroAuto.Equals(
                numeroAuto,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "AIT não encontrado no SITRAN.");
        }

        // RESPONSÁVEL ATUAL DO AIT
        //
        // Enquanto não houver condutor reconhecido pelo DETRAN, o responsável
        // direto é o proprietário. Depois do deferimento/reconhecimento da
        // apresentação de condutor, o responsável direto passa a ser o condutor
        // vinculado àquele processo. Procurador continua sendo uma representação
        // separada e precisa ser informado explicitamente no atendimento.
        var responsavelAtual = await _responsaveis.ObterAtualAsync(auto);

        // Enquanto uma Apresentação de Condutor estiver aguardando retorno
        // do DETRAN, nenhuma pessoa pode abrir Defesa para este AIT.
        _responsaveis.ValidarDefesaLiberada(responsavelAtual);

        var solicitanteEhResponsavel = _responsaveis.DocumentoCorresponde(
            cpf,
            responsavelAtual.Documento);

        var tipoInformado = (
                form.TipoSolicitante ??
                tipoAgendado ??
                string.Empty)
            .Trim()
            .ToUpperInvariant();

        string tipoSolicitante;

        if (solicitanteEhResponsavel)
        {
            // Não confiamos no tipo enviado pelo Front.
            // O tipo vem da responsabilidade atual calculada pelo backend.
            tipoSolicitante = responsavelAtual.Tipo;
        }
        else
        {
            // Um CPF diferente do responsável atual só pode continuar como
            // procurador. Não permitimos selecionar CONDUTOR manualmente para
            // contornar a titularidade definida pelo resultado do DETRAN.
            if (!string.Equals(
                    tipoInformado,
                    "PROCURADOR",
                    StringComparison.Ordinal))
            {
                var responsavelDescricao = responsavelAtual.CondutorReconhecido
                    ? "condutor reconhecido pelo DETRAN"
                    : "proprietário do AIT";

                throw new BusinessException(
                    "O solicitante informado não é o responsável atual pelo AIT. " +
                    $"Para abertura direta de Defesa, o CPF deve corresponder ao {responsavelDescricao}. " +
                    "Se houver representação formal, selecione PROCURADOR.");
            }

            tipoSolicitante = "PROCURADOR";
        }

        if (origem == "SETAP" &&
            !solicitanteEhResponsavel &&
            !string.Equals(
                tipoAgendado,
                "PROCURADOR",
                StringComparison.Ordinal))
        {
            throw new BusinessException(
                "O responsável atual do AIT mudou após o agendamento. " +
                "Confira o CPF do responsável antes de continuar o atendimento.");
        }

        if (form.Arquivos is null ||
            form.TiposDocumento is null ||
            form.Arquivos.Count != form.TiposDocumento.Count ||
            form.TiposDocumento.Any(string.IsNullOrWhiteSpace))
        {
            throw new BusinessException(
                "Os anexos enviados precisam possuir um tipo de documento correspondente.");
        }

        /*
         * DOCUMENTAÇÃO
         *
         * Um documento obrigatório é considerado faltante quando:
         * 1. não possui nenhuma imagem associada; OU
         * 2. o atendente informou no checklist que ele não foi conferido.
         *
         * Se houver falta, a abertura somente é permitida quando o Front envia
         * PermitirFaltaDocumentacao = true após a confirmação do atendente.
         */
        var regrasAtivas = await _regras.ListarAsync(
            ServicoConstants.Defesa,
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
            .Where(tipo => !string.IsNullOrWhiteSpace(tipo))
            .Select(tipo => tipo.Trim().ToUpperInvariant())
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
        {
            throw new BusinessException(
                "Existem documentos obrigatórios não conferidos: " +
                string.Join(", ", faltantes.Select(r => r.NomeDocumento)) +
                ". Confirme a continuidade com restrição por falta de documentação.");
        }

        var documentosFaltantesCodigos = faltaDocumentacao
            ? Limitar1000(
                string.Join(", ", faltantes
                    .Select(r => r.CodigoDocumento.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)))
            : null;

        var documentosFaltantesNomes = faltantes
            .Select(r => r.NomeDocumento.Trim())
            .Where(nomeDocumento => !string.IsNullOrWhiteSpace(nomeDocumento))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // A classificação é refeita com os dados atuais do SITRAN no momento da abertura.
        byte? restricao = auto.Situacao.Equals(
                "PENALIZADO",
                StringComparison.OrdinalIgnoreCase)
            ? (byte)1
            : auto.PrazoDefesa.HasValue &&
              DateOnly.FromDateTime(auto.PrazoDefesa.Value) < dataReferencia
                ? (byte)2
                : form.Estrangeiro
                    ? (byte)3
                    : null;

        long processoId = 0;
        string protocolo = string.Empty;
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
                {
                    throw new BusinessException(
                        "Este agendamento já foi atendido.");
                }

                if (await _processos.ExisteProcessoBloqueanteAsync(
                        auto.NumeroAuto,
                        ServicoConstants.Defesa,
                        connection,
                        transaction))
                {
                    throw new BusinessException(
                        "Já existe um processo ativo para este AIT e serviço.");
                }

                protocolo = await _processos.GerarNumeroProcessoAsync(
                    connection,
                    transaction);

                processoId = await _atendimentos.InserirProcessoPresencialAsync(
                    protocolo,
                    ServicoConstants.Defesa,
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
                    nome,
                    cpf,
                    restricao,
                    form.DataPostagem,
                    faltaDocumentacao,
                    documentosFaltantesCodigos,
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
                    $"Processo {protocolo} aberto presencialmente por atendimento. Origem: {origem}.",
                    connection,
                    transaction);

                if (faltaDocumentacao)
                {
                    var descricaoFalta = Limitar500(
                        "Processo aberto com restrição por falta de documentação. " +
                        "Documentos não conferidos: " +
                        string.Join(", ", documentosFaltantesNomes) + ".");

                    await _movimentacoes.RegistrarAsync(
                        processoId,
                        atendenteId,
                        situacao,
                        "RESTRICAO_FALTA_DOCUMENTACAO",
                        descricaoFalta,
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

                if (processoId > 0 &&
                    !string.IsNullOrWhiteSpace(protocolo))
                {
                    _anexos.RemoverPastaProcesso(protocolo);
                }

                throw new BusinessException(
                    "Este agendamento já foi atendido.");
            }
            catch
            {
                transaction.Rollback();

                if (processoId > 0 &&
                    !string.IsNullOrWhiteSpace(protocolo))
                {
                    _anexos.RemoverPastaProcesso(protocolo);
                }

                throw;
            }
        }

        // O SITRAN decide se aceita a abertura, inclusive quando há restrição.
        // O protocolo local e os anexos são preservados caso a integração não confirme.
        string? mensagemIntegracao = null;
        var integrado = false;
        var rejeitadoSitran = false;

        try
        {
            await _sitran.AbrirDefesaAsync(
                auto.NumeroAuto,
                protocolo,
                dataReferencia);

            integrado = true;
        }
        catch (SitranApiException ex)
        {
            mensagemIntegracao = Limitar500(
                "O SITRAN não confirmou a abertura: " + ex.Message);

            // Respostas 4xx de negócio indicam que o SITRAN recebeu
            // a solicitação e recusou a abertura. Erros de autenticação,
            // timeout e limite de requisições continuam como pendência técnica.
            rejeitadoSitran = EhRejeicaoDoSitran(ex);
        }
        catch (HttpRequestException)
        {
            mensagemIntegracao =
                "Não foi possível comunicar com o SITRAN. Integração pendente.";
        }
        catch (TaskCanceledException)
        {
            mensagemIntegracao =
                "O SITRAN não respondeu no prazo. Integração pendente.";
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
                    "PROCESSO_ENCAMINHADO_ANALISE",
                    faltaDocumentacao
                        ? "Defesa presencial registrada no SITRAN e encaminhada para triagem com restrição por falta de documentação."
                        : "Defesa presencial registrada no SITRAN e encaminhada para triagem.",
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
        else if (rejeitadoSitran &&
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
                    "INTEGRACAO_SITRAN_REJEITADA",
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

    private static bool EhRejeicaoDoSitran(SitranApiException ex)
    {
        var status = (int)ex.StatusCode;

        return status >= 400 &&
               status < 500 &&
               status is not 401 and not 403 and not 408 and not 429;
    }

    private static string Limitar500(string valor)
    {
        var texto = valor.Trim();
        return texto.Length <= 500
            ? texto
            : texto[..500];
    }

    private static string Limitar1000(string valor)
    {
        var texto = valor.Trim();
        return texto.Length <= 1000
            ? texto
            : texto[..1000];
    }

    private static bool DocumentoDoProprietarioCorresponde(
        string cpf,
        string? documento)
    {
        var numeros = SomenteNumeros(documento);

        if (numeros == cpf)
        {
            return true;
        }

        // SITRAN pode devolver CPF de 11 dígitos em campo de 14 posições,
        // completando os três zeros no começo OU no fim.
        if (numeros.Length != 14 || cpf.Length != 11)
        {
            return false;
        }

        return numeros.StartsWith("000", StringComparison.Ordinal) &&
               numeros[3..] == cpf
               || numeros.EndsWith("000", StringComparison.Ordinal) &&
               numeros[..11] == cpf;
    }
}
