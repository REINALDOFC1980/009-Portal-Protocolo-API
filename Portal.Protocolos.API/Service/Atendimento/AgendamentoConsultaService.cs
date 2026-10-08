using Portal.Protocolos.API.Models.Integracao;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class AgendamentoConsultaService
{
    private static readonly IReadOnlyList<AgendamentoAtendimentoResponse> Agendamentos =
    [
        new()
        {
            AgendamentoId = "AG-001",
            NumeroAit = "TESTE00017",
            DataHora = new DateTime(2026, 9, 24, 9, 0, 0),
            CodigoServico = "CONDUTOR",
            CpfSolicitante = "00925565580",
            TipoSolicitante = "PROPRIETARIO"
        },

         new()
        {
            AgendamentoId = "AG-002",
            NumeroAit = "TESTE00013",
            DataHora = new DateTime(2026, 9, 24, 9, 0, 0),
            CodigoServico = "DEFESA",
            CpfSolicitante = "00925565580",
            TipoSolicitante = "PROPRIETARIO"
        },


        new()
        {
            AgendamentoId = "AG-003",
            NumeroAit = "TESTE00012",
            DataHora = new DateTime(2026, 9, 24, 9, 0, 0),
            CodigoServico = "JARI",
            CpfSolicitante = "00925565580",
            TipoSolicitante = "PROPRIETARIO"
        },



            new()
        {
            AgendamentoId = "AG-004",
            NumeroAit = "TESTE00003",
            DataHora = new DateTime(2026, 9, 24, 9, 0, 0),
            CodigoServico = "APRESENTAÇÃO DE CONDUTOR",
            CpfSolicitante = "65752875064",
            TipoSolicitante = "CONDUTOR"
        },

        new()
        {
            AgendamentoId = "AG-005",
            NumeroAit = "TESTE00004",
            DataHora = new DateTime(2026, 9, 24, 10, 30, 0),
            CodigoServico = "DEFESA",
            CpfSolicitante = "65752875064",
            TipoSolicitante = "CONDUTOR"
        },
         new()
        {
            AgendamentoId = "AG-006",
            NumeroAit = "TESTE00011",
            DataHora = new DateTime(2026, 9, 24, 10, 30, 0),
            CodigoServico = "JARI",
            CpfSolicitante = "65752875064",
            TipoSolicitante = "CONDUTOR"
        }
    ];

    public Task<AgendamentoAtendimentoResponse?> ObterPorIdAsync(
        string agendamentoId)
    {
        var agendamento = Agendamentos.FirstOrDefault(a =>
            a.AgendamentoId.Equals(
                agendamentoId.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(agendamento);
    }

    public Task<IReadOnlyList<AgendamentoAtendimentoResponse>> ConsultarAsync(
        string? cpf,
        string? numeroAit)
    {
        var cpfLimpo = new string((cpf ?? "").Where(char.IsDigit).ToArray());
        var ait = numeroAit?.Trim();

        var resultado = Agendamentos
            .Where(a =>
                (cpfLimpo.Length == 0 || a.CpfSolicitante == cpfLimpo) &&
                (string.IsNullOrWhiteSpace(ait) ||
                 a.NumeroAit.Equals(ait, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<AgendamentoAtendimentoResponse>>(
            resultado);
    }
}
