namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class ProcessoPresencialDetalheResponse
{
    public long ProcessoId { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public string NumeroAit { get; set; } = string.Empty;
    public string? Placa { get; set; }
    public int SituacaoProcessoId { get; set; }
    public DateTime DataAbertura { get; set; }
    public short ServicoId { get; set; }
    public int? UsuarioId { get; set; }
    public int AtendenteUsuarioId { get; set; }
    public string AgendamentoId { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string TipoSolicitante { get; set; } = string.Empty;
    public string NomeSolicitante { get; set; } = string.Empty;
    public string DocumentoSolicitante { get; set; } = string.Empty;
    public byte? TipoRestricaoId { get; set; }
    public DateTime DataAtendimento { get; set; }
}
