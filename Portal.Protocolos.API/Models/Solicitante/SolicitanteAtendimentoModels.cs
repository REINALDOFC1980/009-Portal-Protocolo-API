namespace Portal.Protocolos.API.Models.Atendimento.Solicitante;

public sealed class CadastrarSolicitanteRequest
{
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string? RegistroCnh { get; set; }
    public string? UfCnh { get; set; }
    public byte? ModeloCnh { get; set; }
}

public sealed class SolicitanteAtendimentoResponse
{
    public int UsuarioId { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string? RegistroCnh { get; set; }
    public string? UfCnh { get; set; }
    public byte? ModeloCnh { get; set; }
}

public sealed class ConsultaSolicitanteResponse
{
    public bool Cadastrado { get; set; }
    public SolicitanteAtendimentoResponse? Usuario { get; set; }
}
