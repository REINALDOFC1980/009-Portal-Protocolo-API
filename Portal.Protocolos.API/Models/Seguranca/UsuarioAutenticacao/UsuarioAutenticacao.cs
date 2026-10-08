namespace Portal.Protocolos.API.Models.Seguranca.UsuarioAutenticacao;

public sealed class UsuarioAutenticacao
{
    public int UsuarioAutenticacaoId { get; set; }

    public int UsuarioId { get; set; }

    public short TipoAutenticacaoId { get; set; }

    public string? SenhaHash { get; set; }

    public string? IdentificadorExterno { get; set; }

    public short TentativasFalhas { get; set; }

    public DateTime? BloqueadoAte { get; set; }

    public DateTime? UltimoAcesso { get; set; }

    public DateTime DataCadastro { get; set; }

    public DateTime? DataAlteracao { get; set; }

    public bool Ativo { get; set; }
}
