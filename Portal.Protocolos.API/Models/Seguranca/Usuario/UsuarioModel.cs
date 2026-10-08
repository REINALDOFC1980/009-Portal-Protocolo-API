namespace Portal.Protocolos.API.Models.Seguranca.Usuario
{
    public sealed class UsuarioModel
    {
        public int UsuarioId { get; set; }

        public short TipoPessoaId { get; set; }

        public short? TipoPessoaJuridicaId { get; set; }

        public short StatusUsuarioId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? NomeFantasia { get; set; }

        public string Documento { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Celular { get; set; }

        public string? RegistroCnh { get; set; }

        public string? UfCnh { get; set; }

        public byte? ModeloCnh { get; set; }

        public DateTime DataCadastro { get; set; }

        public DateTime? DataAlteracao { get; set; }
    }
}
