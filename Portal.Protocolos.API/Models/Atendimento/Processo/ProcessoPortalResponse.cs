namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class ProcessoPortalResponse
{
    public long ProcessoId { get; set; }

    public string Protocolo { get; set; } = string.Empty;

    public string NumeroAuto { get; set; } = string.Empty;

    public string Servico { get; set; } = string.Empty;

    public string ServicoCodigo { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public string Situacao { get; set; } = string.Empty;

    // WEB | PRESENCIAL
    public string Origem { get; set; } = string.Empty;

    public string? DocumentoCondutor { get; set; }

    public string? NomeCondutor { get; set; }

    public bool? CondutorAceito { get; set; }

    public DateTime? DataIndicacaoCondutor { get; set; }

    public DateTime? DataAceiteCondutor { get; set; }

    public DateTime? DataRecusaCondutor { get; set; }

    public string? MotivoRecusaCondutor { get; set; }

    public string? Resultado { get; set; }

    public string? CodigoResultado { get; set; }

    public DateTime? DataJulgamento { get; set; }

    public DateTime? DataPublicacaoResultado { get; set; }

    public string? RetornoDetran { get; set; }
}
