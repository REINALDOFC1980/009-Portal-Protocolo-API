namespace Portal.Protocolos.API.Models.Cadastro.Servico.Entity
{
    public sealed class Servico
    {
        public short ServicoId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public bool ExigeCondutor { get; set; }

        public bool Ativo { get; set; }

        public short Ordem { get; set; }

        public DateTime DataCadastro { get; set; }
    }
}
