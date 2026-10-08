namespace Portal.Protocolos.API.Models.Seguranca.UsuarioPerfil
{
    public sealed class UsuarioPerfil
    {
        public int UsuarioPerfilId { get; set; }

        public int UsuarioId { get; set; }

        public short PerfilId { get; set; }

        public DateTime DataCadastro { get; set; }

        public bool Ativo { get; set; }
    }
}
