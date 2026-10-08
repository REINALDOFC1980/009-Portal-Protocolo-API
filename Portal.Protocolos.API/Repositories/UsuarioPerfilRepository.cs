using Dapper;
using Portal.Protocolos.API.Models.Seguranca.UsuarioPerfil;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class UsuarioPerfilRepository
{
    private readonly DbSession _dbSession;

    public UsuarioPerfilRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task InserirAsync(UsuarioPerfil usuarioPerfil)
    {
        const string sql = @"
            INSERT INTO UsuarioPerfil
            (
                UsuarioId,
                PerfilId,
                Ativo
            )
            VALUES
            (
                @UsuarioId,
                @PerfilId,
                @Ativo
            );";

        using var connection = _dbSession.CreateConnection();

        await connection.ExecuteAsync(
            sql,
            usuarioPerfil);
    }

    public async Task<IReadOnlyList<string>> ListarCodigosAtivosAsync(int usuarioId)
    {
        const string sql = """
        SELECT DISTINCT P.Codigo
        FROM dbo.UsuarioPerfil UP
        INNER JOIN dbo.Perfil P
            ON P.PerfilId = UP.PerfilId
        WHERE UP.UsuarioId = @UsuarioId
          AND UP.Ativo = 1
          AND P.Ativo = 1;
        """;

        using var connection = _dbSession.CreateConnection();

        var perfis = await connection.QueryAsync<string>(
            sql,
            new { UsuarioId = usuarioId });

        return perfis.ToList();
    }
}
