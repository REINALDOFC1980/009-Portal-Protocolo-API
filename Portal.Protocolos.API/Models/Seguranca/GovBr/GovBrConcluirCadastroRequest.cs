namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrConcluirCadastroRequest
{
    public string TokenCadastro { get; set; } =
        string.Empty;

    public short TipoPessoaId { get; set; }

    public string? Cnpj { get; set; }

    public string? RazaoSocial { get; set; }

    public string? NomeFantasia { get; set; }

    public short? TipoPessoaJuridicaId { get; set; }

    public string Email { get; set; } =
        string.Empty;

    public string? Celular { get; set; }

    public string? RegistroCnh { get; set; }

    public string? UfCnh { get; set; }

    public byte? ModeloCnh { get; set; }
}
