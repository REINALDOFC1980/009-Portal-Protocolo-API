namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class ProcessoCondutor
{
    public long ProcessoCondutorId { get; set; }

    public long ProcessoId { get; set; }

    public string DocumentoCondutor { get; set; } = string.Empty;

    public string? NomeCondutor { get; set; }

    public string? RegistroCnh { get; set; }

    public string? UfCnh { get; set; }

    public string? ModeloCnh { get; set; }

    public int? UsuarioCondutorId { get; set; }

    public bool? Aceito { get; set; }

    public DateTime DataIndicacao { get; set; }

    public DateTime? DataAceite { get; set; }

    public DateTime? DataRecusa { get; set; }

    public string? MotivoRecusa { get; set; }
}
