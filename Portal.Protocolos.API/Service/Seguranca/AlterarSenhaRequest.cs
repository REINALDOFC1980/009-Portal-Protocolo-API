namespace Portal.Protocolos.API.Models.Seguranca.Usuario;

public sealed class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;

    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
