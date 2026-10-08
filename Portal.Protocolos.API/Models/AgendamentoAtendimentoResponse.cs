namespace Portal.Protocolos.API.Models.Integracao;

public sealed class AgendamentoAtendimentoResponse
{
    public string AgendamentoId { get; set; } = string.Empty;
    public string NumeroAit { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public string CodigoServico { get; set; } = string.Empty;
    public string CpfSolicitante { get; set; } = string.Empty;
    public string TipoSolicitante { get; set; } = string.Empty;
}
