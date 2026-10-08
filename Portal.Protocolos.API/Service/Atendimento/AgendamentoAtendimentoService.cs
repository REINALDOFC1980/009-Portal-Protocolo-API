using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class AgendamentoAtendimentoService
{
    private readonly AgendamentoConsultaService _agendamentos;
    private readonly SitranApiClient _sitran;
    private readonly RegraAnexoRepository _regras;

    public AgendamentoAtendimentoService(
        AgendamentoConsultaService agendamentos,
        SitranApiClient sitran,
        RegraAnexoRepository regras)
    {
        _agendamentos = agendamentos;
        _sitran = sitran;
        _regras = regras;
    }

    public Task<IReadOnlyList<AgendamentoAtendimentoResponse>> ConsultarAsync(
        string? cpf, string? numeroAit) => _agendamentos.ConsultarAsync(cpf, numeroAit);

    public async Task<ResultadoConsultaAitAgendado> ObterAitAsync(
        string agendamentoId, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentos.ObterPorIdAsync(agendamentoId);
        if (agendamento is null)
            return ResultadoConsultaAitAgendado.Erro(404, "Agendamento não encontrado.");

        var auto = await _sitran.ObterAitAsync(agendamento.NumeroAit, cancellationToken);
        if (auto is null)
            return ResultadoConsultaAitAgendado.Erro(
                404, $"AIT {agendamento.NumeroAit} não encontrado no SITRAN.");

        if (!auto.NumeroAuto.Equals(agendamento.NumeroAit, StringComparison.OrdinalIgnoreCase))
            return ResultadoConsultaAitAgendado.Erro(
                502, "O SITRAN retornou um AIT diferente do agendado.");

        var alertas = new List<string>();
        int? tipoRestricaoId = null;
        string? tipoRestricao = null;

        if (auto.Situacao.Equals("PENALIZADO", StringComparison.OrdinalIgnoreCase))
        {
            tipoRestricaoId = 1;
            tipoRestricao = "PENALIZADO";
            alertas.Add("O AIT consta como penalizado no SITRAN.");
        }
        else if (agendamento.CodigoServico.Equals("DEFESA", StringComparison.OrdinalIgnoreCase) &&
                 auto.PrazoDefesa.HasValue && auto.PrazoDefesa.Value.Date < DateTime.Today)
        {
            tipoRestricaoId = 2;
            tipoRestricao = "INTEMPESTIVO";
            alertas.Add("O prazo para defesa consta como vencido no SITRAN.");
        }

        var proprietario = CpfCorrespondeAoDocumento(
            agendamento.CpfSolicitante, auto.DocumentoProprietario);

        if (agendamento.TipoSolicitante.Equals("PROPRIETARIO", StringComparison.OrdinalIgnoreCase) &&
            !proprietario)
            alertas.Add("O CPF do solicitante agendado não corresponde ao documento do proprietário no SITRAN. Confira a representação antes de abrir o processo.");

        var tipoEfetivo = proprietario ? "PROPRIETARIO" : agendamento.TipoSolicitante.Trim().ToUpperInvariant();
        var servicoId = IdentificarServico(agendamento.CodigoServico);
        IReadOnlyList<string> documentos = Array.Empty<string>();
        if (servicoId.HasValue)
        {
            var regras = await _regras.ListarAsync(servicoId.Value, "SETAP", tipoEfetivo, false);
            documentos = regras.Select(r => r.NomeDocumento).ToList();
        }

        return ResultadoConsultaAitAgendado.Sucesso(new DadosAitAgendado
        {
            Agendamento = agendamento,
            AutoInfracao = auto,
            Analise = new AnaliseAitAgendado
            {
                TipoRestricaoId = tipoRestricaoId,
                TipoRestricao = tipoRestricao,
                Alertas = alertas,
                DocumentosOrientativos = documentos
            }
        });
    }

    private static short? IdentificarServico(string codigo) =>
        codigo.Trim().ToUpperInvariant() switch
        {
            "DEFESA" => ServicoConstants.Defesa,
            "CONDUTOR" => ServicoConstants.Condutor,
            "JARI" => ServicoConstants.Jari,
            "CETRAN" => ServicoConstants.Cetran,
            "RESSARCIMENTO" => ServicoConstants.Ressarcimento,
            _ => null
        };

    private static bool CpfCorrespondeAoDocumento(string? cpfSolicitante, string? documentoProprietario)
    {
        var cpf = SomenteNumeros(cpfSolicitante);
        var documento = SomenteNumeros(documentoProprietario);
        if (cpf.Length != 11) return false;
        if (documento.Length == 11) return documento == cpf;
        if (documento.Length != 14) return false;
        return (documento.StartsWith("000", StringComparison.Ordinal) && documento[^11..] == cpf) ||
               (documento.EndsWith("000", StringComparison.Ordinal) && documento[..11] == cpf);
    }

    private static string SomenteNumeros(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}

public sealed class ResultadoConsultaAitAgendado
{
    public int? CodigoErro { get; private init; }
    public string? MensagemErro { get; private init; }
    public DadosAitAgendado? Dados { get; private init; }

    public static ResultadoConsultaAitAgendado Erro(int codigo, string mensagem) =>
        new() { CodigoErro = codigo, MensagemErro = mensagem };

    public static ResultadoConsultaAitAgendado Sucesso(DadosAitAgendado dados) =>
        new() { Dados = dados };
}

public sealed class DadosAitAgendado
{
    public AgendamentoAtendimentoResponse Agendamento { get; set; } = new();
    public AutoInfracaoResponse AutoInfracao { get; set; } = new();
    public AnaliseAitAgendado Analise { get; set; } = new();
}

public sealed class AnaliseAitAgendado
{
    public int? TipoRestricaoId { get; set; }
    public string? TipoRestricao { get; set; }
    public IReadOnlyList<string> Alertas { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DocumentosOrientativos { get; set; } = Array.Empty<string>();
}
