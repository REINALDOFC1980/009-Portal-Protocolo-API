namespace Portal.Protocolos.API.Models.Seguranca.Login;

public sealed class LoginRequest
{
    public string Documento { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}
