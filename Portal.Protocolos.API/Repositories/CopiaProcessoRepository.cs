using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Copia;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class CopiaProcessoRepository
{
    private readonly DbSession _db;

    public CopiaProcessoRepository(DbSession db)
    {
        _db = db;
    }

    public async Task<CopiaProcessoResponse?> ObterPorNumeroAsync(
        string numeroProcesso)
    {
        const string sql = """
            SELECT TOP (1)
                P.ProcessoId,
                P.NumeroProcesso,
                P.NumeroAIT AS NumeroAit,
                S.Codigo AS CodigoServico,
                S.Nome AS Descricao,
                PA.Origem,
                PA.NomeSolicitante AS Solicitante,
                PA.DocumentoSolicitante AS Cpf,
                P.DataAbertura
            FROM dbo.Processo P
            INNER JOIN dbo.ProcessoAtendimento PA
                ON PA.ProcessoId = P.ProcessoId
            INNER JOIN dbo.Servico S
                ON S.ServicoId = P.ServicoId
            WHERE UPPER(LTRIM(RTRIM(P.NumeroProcesso))) = @NumeroProcesso;
            """;

        using var connection = _db.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<CopiaProcessoResponse>(
            sql,
            new { NumeroProcesso = numeroProcesso.Trim().ToUpperInvariant() });
    }
}
