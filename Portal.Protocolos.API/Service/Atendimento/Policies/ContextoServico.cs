using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Services.Integracao;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class ContextoServico
{
    public required AutoInfracaoResponse Auto { get; set; }

    // ============================================================
    // ELEGIBILIDADE SITRAN
    // ============================================================

    public SitranElegibilidadeResponse? ElegibilidadeSitran { get; set; }

    // ============================================================
    // JARI / CETRAN
    // ============================================================

    public bool PossuiJari { get; set; }

    public bool JariJulgada { get; set; }

    public string? ResultadoJari { get; set; }

    public DateTime? DataJulgamentoJari { get; set; }

    public DateTime? DataNotificacaoJari { get; set; }

    public DateTime? PrazoCetran { get; set; }

    public bool PossuiCetran { get; set; }

    // ============================================================
    // PAGAMENTO / RESSARCIMENTO
    // ============================================================

    public bool PossuiPagamento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public decimal? ValorPago { get; set; }

    public int QuantidadePagamentos { get; set; }

    public bool AutoCancelado { get; set; }

    public bool PagamentoDuplicado =>
        QuantidadePagamentos > 1;

    public bool PossuiMotivoRessarcimento =>
        PossuiPagamento &&
        (
            PagamentoDuplicado ||
            AutoCancelado
        );

    // ============================================================
    // PROCESSOS JÁ ABERTOS NO PORTAL
    // ============================================================

    public bool PossuiApresentacaoCondutor { get; set; }

    public bool PossuiDefesa { get; set; }

    public bool PossuiProcessoJari { get; set; }

    public bool PossuiProcessoCetran { get; set; }

    public bool PossuiProcessoRessarcimento { get; set; }
}
