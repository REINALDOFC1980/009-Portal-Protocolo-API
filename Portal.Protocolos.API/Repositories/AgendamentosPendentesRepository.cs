using Dapper;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class AgendamentosPendentesRepository
{
    private readonly DbSession _dbSession;

    public AgendamentosPendentesRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<(HashSet<string> Atendidos, HashSet<(string NumeroAit, short ServicoId)> Bloqueados)>
        ConsultarImpedimentosAsync(IReadOnlyList<AgendamentoAtendimentoResponse> agendamentos)
    {
        var atendidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bloqueados = new HashSet<(string NumeroAit, short ServicoId)>();
        if (agendamentos.Count == 0)
            return (atendidos, bloqueados);

        var ids = agendamentos.Select(a => a.AgendamentoId).Distinct().ToArray();
        var aits = agendamentos.Select(a => a.NumeroAit).Distinct().ToArray();

        const string sql = """
            SELECT DISTINCT AgendamentoId
            FROM dbo.ProcessoAtendimento
            WHERE AgendamentoId IN @Ids;

            SELECT DISTINCT P.NumeroAIT, P.ServicoId
            FROM dbo.Processo P
            INNER JOIN dbo.SituacaoProcesso SP
                ON SP.SituacaoProcessoId = P.SituacaoProcessoId
            WHERE P.NumeroAIT IN @Aits AND SP.BloqueiaNovaAbertura = 1;
            """;

        using var connection = _dbSession.CreateConnection();
        using var resultado = await connection.QueryMultipleAsync(sql, new { Ids = ids, Aits = aits });

        foreach (var id in await resultado.ReadAsync<string>())
            atendidos.Add(id);

        foreach (var item in await resultado.ReadAsync<ProcessoBloqueante>())
            bloqueados.Add((item.NumeroAIT.ToUpperInvariant(), item.ServicoId));

        return (atendidos, bloqueados);
    }

    private sealed class ProcessoBloqueante
    {
        public string NumeroAIT { get; set; } = string.Empty;
        public short ServicoId { get; set; }
    }
}
