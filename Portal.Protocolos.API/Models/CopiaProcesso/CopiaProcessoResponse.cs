namespace Portal.Protocolos.API.Models.Atendimento.Copia;

public sealed class CopiaProcessoResponse
{
    public long ProcessoId { get; set; }
    public string NumeroProcesso { get; set; } = string.Empty;
    public string NumeroAit { get; set; } = string.Empty;
    public string CodigoServico { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public List<CopiaProcessoAnexoResponse> Anexos { get; set; } = [];
}

public sealed class CopiaProcessoAnexoResponse
{
    public long ProcessoAnexoId { get; set; }
    public string NomeOriginal { get; set; } = string.Empty;
    public string Extensao { get; set; } = string.Empty;
    public string? TipoDocumento { get; set; }
    public DateTime DataCadastro { get; set; }
    public bool Imagem { get; set; }
}

public sealed class CopiaProcessoArquivoResponse
{
    public byte[] Conteudo { get; set; } = [];
    public string ContentType { get; set; } = "application/octet-stream";
    public string NomeArquivo { get; set; } = string.Empty;
}
