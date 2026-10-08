using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class ProcessoPresencialDetalheRepository
{
    private readonly DbSession _dbSession;

    public ProcessoPresencialDetalheRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<ProcessoPresencialDetalheResponse?> ObterAsync(long processoId)
    {
        const string sql = """
            SELECT P.ProcessoId,
                   P.NumeroProcesso AS Protocolo,
                   P.NumeroAIT AS NumeroAit,
                   P.Placa,
                   P.SituacaoProcessoId,
                   P.DataAbertura,
                   P.ServicoId,
                   P.UsuarioId,
                   PA.AtendenteUsuarioId,
                   PA.AgendamentoId,
                   PA.Origem,
                   PA.TipoSolicitante,
                   PA.NomeSolicitante,
                   PA.DocumentoSolicitante,
                   PA.TipoRestricaoId,
                   PA.DataAtendimento
            FROM dbo.Processo P
            INNER JOIN dbo.ProcessoAtendimento PA ON PA.ProcessoId = P.ProcessoId
            WHERE P.ProcessoId = @ProcessoId;
            """;

        using var connection = _dbSession.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProcessoPresencialDetalheResponse>(
            sql, new { ProcessoId = processoId });
    }
}
