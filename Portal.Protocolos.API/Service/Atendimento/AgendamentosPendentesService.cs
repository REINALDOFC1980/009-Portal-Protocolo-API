using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class AgendamentosPendentesService
{
    private readonly AgendamentoConsultaService _agendamentos;
    private readonly AgendamentosPendentesRepository _repository;

    public AgendamentosPendentesService(
        AgendamentoConsultaService agendamentos,
        AgendamentosPendentesRepository repository)
    {
        _agendamentos = agendamentos;
        _repository = repository;
    }

    public async Task<IReadOnlyList<AgendamentoAtendimentoResponse>> ConsultarAsync(
        string? cpf, string? numeroAit)
    {
        if (string.IsNullOrWhiteSpace(cpf) && string.IsNullOrWhiteSpace(numeroAit))
            throw new BusinessException("Informe o CPF ou o número do AIT.");

        var encontrados = await _agendamentos.ConsultarAsync(cpf, numeroAit);
        var (atendidos, bloqueados) = await _repository.ConsultarImpedimentosAsync(encontrados);

        return encontrados.Where(item =>
        {
            if (atendidos.Contains(item.AgendamentoId)) return false;
            var servicoId = IdentificarServico(item.CodigoServico);
            return servicoId.HasValue &&
                !bloqueados.Contains((item.NumeroAit.ToUpperInvariant(), servicoId.Value));
        }).ToList();
    }

    private static short? IdentificarServico(string codigo) =>
        codigo.Trim().ToUpperInvariant() switch
        {
            "DEFESA" => ServicoConstants.Defesa,
            "CONDUTOR" => ServicoConstants.Condutor,
            "JARI" => ServicoConstants.Jari,
            "CETRAN" => ServicoConstants.Cetran,
            "RESSARCIMENTO" => ServicoConstants.Ressarcimento,
            _ => null
        };
}
