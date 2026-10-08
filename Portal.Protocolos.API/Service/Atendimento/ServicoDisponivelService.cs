using Portal.Protocolos.API.Models.Atendimento.Ait;
using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.API.Models.Cadastros.Servico.Response;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class ServicoDisponivelService
{
    private readonly ServicoRepository _servicoRepository;
    private readonly ProcessoRepository _processoRepository;
    private readonly SitranApiClient _sitranApiClient;
    private readonly ResponsavelAitService _responsavelAitService;
    private readonly List<IServicoPolicy> _policies;

    public ServicoDisponivelService(
        ServicoRepository servicoRepository,
        ProcessoRepository processoRepository,
        SitranApiClient sitranApiClient,
        ResponsavelAitService responsavelAitService)
    {
        _servicoRepository = servicoRepository;
        _processoRepository = processoRepository;
        _sitranApiClient = sitranApiClient;
        _responsavelAitService = responsavelAitService;

        _policies = PolicyFactory.Criar();
    }

    public async Task<List<ServicoDisponivelResponse>> AvaliarAsync(
        ContextoServico contexto)
    {
        await CarregarElegibilidadeSitranAsync(contexto);

        var responsavelAtual =
            await _responsavelAitService.ObterAtualAsync(contexto.Auto);

        var processosBloqueantes =
            await CarregarProcessosExistentesAsync(contexto);

        await CarregarContextoJariAsync(contexto);

        var servicos =
            await _servicoRepository.ListarAtivosAsync();

        var resultado =
            new List<ServicoDisponivelResponse>();

        foreach (var servico in servicos)
        {
            processosBloqueantes.TryGetValue(
                servico.ServicoId,
                out var processoVinculado);

            var policy =
                _policies.FirstOrDefault(
                    x => x.ServicoId == servico.ServicoId);

            if (policy is null)
            {
                resultado.Add(
                    new ServicoDisponivelResponse
                    {
                        ServicoId = servico.ServicoId,
                        Codigo = servico.Codigo,
                        Nome = servico.Nome,
                        Descricao = servico.Descricao,
                        ExigeCondutor = servico.ExigeCondutor,
                        Disponivel = false,
                        Motivo =
                            "Serviço ainda não possui regra de disponibilidade configurada.",
                        ProcessoVinculado = processoVinculado
                    });

                continue;
            }

            var avaliacao =
                policy.Avaliar(contexto);

            var disponivel = avaliacao.Disponivel;
            var motivo = avaliacao.Motivo;

            // REGRA TRANSVERSAL DA DEFESA
            //
            // Havendo Apresentação de Condutor aguardando decisão do DETRAN,
            // a Defesa fica indisponível para qualquer pessoa, independentemente
            // de a policy normal do rito indicar que o prazo/situação permitiria.
            if (servico.ServicoId == ServicoConstants.Defesa &&
                responsavelAtual.DefesaBloqueada)
            {
                disponivel = false;
                motivo = responsavelAtual.MotivoBloqueioDefesa;
            }

            resultado.Add(
                new ServicoDisponivelResponse
                {
                    ServicoId = servico.ServicoId,
                    Codigo = servico.Codigo,
                    Nome = servico.Nome,
                    Descricao = servico.Descricao,
                    ExigeCondutor = servico.ExigeCondutor,
                    Disponivel = disponivel,
                    Motivo = motivo,
                    Observacao = avaliacao.Observacao,
                    Alertas = avaliacao.Alertas,
                    ProcessoVinculado = processoVinculado
                });
        }

        return resultado;
    }

    private async Task CarregarElegibilidadeSitranAsync(
        ContextoServico contexto)
    {
        contexto.ElegibilidadeSitran =
            await _sitranApiClient.ObterElegibilidadeAsync(
                contexto.Auto.NumeroAuto);
    }

    private async Task<Dictionary<short, ProcessoBloqueanteResponse>>
        CarregarProcessosExistentesAsync(
            ContextoServico contexto)
    {
        var numeroAuto =
            contexto.Auto.NumeroAuto;

        // Recupera o processo mais recente de cada serviço para o AIT,
        // independentemente de a situação bloquear ou não uma nova abertura.
        // Isso permite informar ao atendente que já existe um processo
        // vinculado, mesmo quando a situação atual não é bloqueante.
        var existentes =
            await _processoRepository.ListarUltimosProcessosPorAitAsync(
                numeroAuto);

        var processos =
            existentes
                .GroupBy(x => x.ServicoId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo
                        .OrderByDescending(x => x.DataAbertura)
                        .ThenByDescending(x => x.ProcessoId)
                        .First());

        processos.TryGetValue(
            ServicoConstants.Defesa,
            out var defesa);

        contexto.PossuiDefesa =
            defesa?.BloqueiaNovaAbertura == true;

        processos.TryGetValue(
            ServicoConstants.Condutor,
            out var condutor);

        contexto.PossuiApresentacaoCondutor =
            condutor?.BloqueiaNovaAbertura == true;

        processos.TryGetValue(
            ServicoConstants.Jari,
            out var jari);

        contexto.PossuiProcessoJari =
            jari?.BloqueiaNovaAbertura == true;

        processos.TryGetValue(
            ServicoConstants.Cetran,
            out var cetran);

        contexto.PossuiProcessoCetran =
            cetran?.BloqueiaNovaAbertura == true;

        processos.TryGetValue(
            ServicoConstants.Ressarcimento,
            out var ressarcimento);

        contexto.PossuiProcessoRessarcimento =
            ressarcimento?.BloqueiaNovaAbertura == true;

        return processos;
    }

    private async Task CarregarContextoJariAsync(
        ContextoServico contexto)
    {
        var jari =
            await _processoRepository.ObterJulgamentoJariAsync(
                contexto.Auto.NumeroAuto);

        if (jari is null)
        {
            contexto.PossuiJari = false;
            contexto.JariJulgada = false;

            return;
        }

        contexto.PossuiJari = true;

        contexto.JariJulgada =
            jari.DataJulgamento.HasValue &&
            !string.IsNullOrWhiteSpace(
                jari.Resultado);

        contexto.ResultadoJari =
            jari.Resultado;

        contexto.DataJulgamentoJari =
            jari.DataJulgamento;

        contexto.DataNotificacaoJari =
            jari.DataNotificacao;

        contexto.PrazoCetran =
            jari.PrazoRecurso;
    }
}
