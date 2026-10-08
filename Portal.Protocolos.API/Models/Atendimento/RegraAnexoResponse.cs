namespace Portal.Protocolos.API.Models.Atendimento.Anexo;

public sealed class RegraAnexoResponse
{
    public int RegraAnexoId { get; set; }
    public short ServicoId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string? TipoSolicitante { get; set; }
    public string CodigoDocumento { get; set; } = string.Empty;
    public string NomeDocumento { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
    public bool Ativo { get; set; }
    public short Ordem { get; set; }
}

public sealed class RegraAnexoSalvarRequest
{
    public short ServicoId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string? TipoSolicitante { get; set; }
    public string CodigoDocumento { get; set; } = string.Empty;
    public string NomeDocumento { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; } = true;
    public bool Ativo { get; set; } = true;
    public short Ordem { get; set; }
}
