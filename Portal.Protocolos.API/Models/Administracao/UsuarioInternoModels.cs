namespace Portal.Protocolos.API.Models.Atendimento.Administracao;

public sealed class UsuarioInternoResponse
{
    public int UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCadastro { get; set; }
}

public sealed class CriarUsuarioInternoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public sealed class AtualizarUsuarioInternoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}

public sealed class AlterarStatusUsuarioInternoRequest
{
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}

public sealed class RedefinirSenhaUsuarioInternoRequest
{
    public string NovaSenha { get; set; } = string.Empty;
}
