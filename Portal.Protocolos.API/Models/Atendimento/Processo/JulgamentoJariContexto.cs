namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class JulgamentoJariContexto
{
    public long ProcessoId { get; set; }

    public string? Resultado { get; set; }

    public DateTime? DataJulgamento { get; set; }

    public DateTime? DataNotificacao { get; set; }

    public DateTime? PrazoRecurso { get; set; }
}
