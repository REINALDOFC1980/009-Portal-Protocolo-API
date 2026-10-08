using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class RegraAnexoRepository
{
    private readonly DbSession _db;

    public RegraAnexoRepository(DbSession db) => _db = db;

    public async Task<IReadOnlyList<string>> ListarCodigosObrigatoriosAsync(
        short servicoId, string origem, string tipoSolicitante)
    {
        const string sql = """
            SELECT CodigoDocumento
            FROM dbo.RegraAnexo
            WHERE ServicoId = @ServicoId AND Origem = @Origem
              AND Ativo = 1 AND Obrigatorio = 1
              AND (TipoSolicitante IS NULL OR TipoSolicitante = @TipoSolicitante);
            """;

        using var connection = _db.CreateConnection();
        var codigos = await connection.QueryAsync<string>(sql,
            new { ServicoId = servicoId, Origem = origem, TipoSolicitante = tipoSolicitante });
        return codigos.ToList();
    }

    public async Task<IReadOnlyList<RegraAnexoResponse>> ListarAsync(
        short servicoId, string origem, string? tipoSolicitante, bool incluirInativos)
    {
        const string sql = """
            SELECT RegraAnexoId, ServicoId, Origem, TipoSolicitante,
                   CodigoDocumento, NomeDocumento, Obrigatorio, Ativo, Ordem
            FROM dbo.RegraAnexo
            WHERE ServicoId = @ServicoId AND Origem = @Origem
              AND (@IncluirInativos = 1 OR Ativo = 1)
              AND (@TipoSolicitante IS NULL OR TipoSolicitante IS NULL OR TipoSolicitante = @TipoSolicitante)
            ORDER BY Ordem, NomeDocumento;
            """;

        using var connection = _db.CreateConnection();
        var regras = await connection.QueryAsync<RegraAnexoResponse>(sql,
            new { ServicoId = servicoId, Origem = origem, TipoSolicitante = tipoSolicitante, IncluirInativos = incluirInativos });
        return regras.ToList();
    }

    public async Task<int> CriarAsync(RegraAnexoSalvarRequest request)
    {
        const string sql = """
            INSERT INTO dbo.RegraAnexo
                (ServicoId, Origem, TipoSolicitante, CodigoDocumento,
                 NomeDocumento, Obrigatorio, Ativo, Ordem)
            VALUES
                (@ServicoId, @Origem, @TipoSolicitante, @CodigoDocumento,
                 @NomeDocumento, @Obrigatorio, @Ativo, @Ordem);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using var connection = _db.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, request);
    }

    public async Task<bool> AtualizarAsync(int id, RegraAnexoSalvarRequest request)
    {
        const string sql = """
            UPDATE dbo.RegraAnexo
            SET ServicoId = @ServicoId, Origem = @Origem,
                TipoSolicitante = @TipoSolicitante,
                CodigoDocumento = @CodigoDocumento, NomeDocumento = @NomeDocumento,
                Obrigatorio = @Obrigatorio, Ativo = @Ativo, Ordem = @Ordem
            WHERE RegraAnexoId = @Id;
            """;

        using var connection = _db.CreateConnection();
        return await connection.ExecuteAsync(sql, new
        {
            Id = id,
            request.ServicoId,
            request.Origem,
            request.TipoSolicitante,
            request.CodigoDocumento,
            request.NomeDocumento,
            request.Obrigatorio,
            request.Ativo,
            request.Ordem
        }) > 0;
    }
}
