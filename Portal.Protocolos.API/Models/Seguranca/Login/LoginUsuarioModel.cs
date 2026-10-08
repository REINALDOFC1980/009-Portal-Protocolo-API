namespace Portal.Protocolos.API.Models.Seguranca.Login;

public sealed class LoginUsuarioModel
{
    public int UsuarioId { get; set; }

    public short TipoPessoaId { get; set; }

    public short? TipoPessoaJuridicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Documento { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public bool Ativo { get; set; }
}
