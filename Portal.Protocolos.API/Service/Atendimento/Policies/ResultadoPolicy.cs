namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class ResultadoPolicy
{
    public bool Disponivel { get; set; }

    public string? Motivo { get; set; }

    public string? Observacao { get; set; }

    public List<string> Alertas { get; set; } = [];
}
