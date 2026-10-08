namespace Portal.Protocolos.API.Models.Seguranca.Login;

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Documento { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public short TipoPessoaId { get; set; }

    public short? TipoPessoaJuridicaId { get; set; }
}
