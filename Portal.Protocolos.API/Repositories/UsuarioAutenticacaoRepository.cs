using Dapper;
using Portal.Protocolos.API.Models.Seguranca.Login;
using Portal.Protocolos.API.Models.Seguranca.UsuarioAutenticacao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Repositories;

public sealed class UsuarioAutenticacaoRepository
{
    private readonly DbSession _dbSession;

    public UsuarioAutenticacaoRepository(
        DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task InserirAsync(
        UsuarioAutenticacao usuarioAutenticacao)
    {
        const string sql = @"
            INSERT INTO UsuarioAutenticacao
            (
                UsuarioId,
                TipoAutenticacaoId,
                SenhaHash,
                IdentificadorExterno,
                TentativasFalhas,
                BloqueadoAte,
                UltimoAcesso,
                DataAlteracao,
                Ativo
            )
            VALUES
            (
                @UsuarioId,
                @TipoAutenticacaoId,
                @SenhaHash,
                @IdentificadorExterno,
                @TentativasFalhas,
                @BloqueadoAte,
                @UltimoAcesso,
                @DataAlteracao,
                @Ativo
            );";

        using var connection =
            _dbSession.CreateConnection();

        await connection.ExecuteAsync(
            sql,
            usuarioAutenticacao);
    }

    public async Task<LoginUsuarioModel?>
        ObterLoginAsync(
            string documento)
    {
        const string sql = @"
            SELECT
                U.UsuarioId,
                U.TipoPessoaId,
                U.TipoPessoaJuridicaId,
                U.Nome,
                U.Documento,
                U.Email,
                UA.SenhaHash,
                UA.Ativo
            FROM Usuario U
            INNER JOIN UsuarioAutenticacao UA
                ON UA.UsuarioId = U.UsuarioId
            WHERE U.Documento = @Documento
              AND UA.TipoAutenticacaoId = @TipoAutenticacaoId
              AND UA.Ativo = 1;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<LoginUsuarioModel>(
                sql,
                new
                {
                    Documento =
                        documento,

                    TipoAutenticacaoId =
                        TipoAutenticacaoConstants.Local
                });
    }

    public async Task<LoginUsuarioModel?>
        ObterLoginGovBrAsync(
            string identificadorExterno)
    {
        const string sql = @"
            SELECT TOP 1
                U.UsuarioId,
                U.TipoPessoaId,
                U.TipoPessoaJuridicaId,
                U.Nome,
                U.Documento,
                U.Email,
                UA.SenhaHash,
                UA.Ativo
            FROM UsuarioAutenticacao UA
            INNER JOIN Usuario U
                ON U.UsuarioId = UA.UsuarioId
            WHERE UA.TipoAutenticacaoId =
                    @TipoAutenticacaoId
              AND UA.IdentificadorExterno =
                    @IdentificadorExterno
              AND UA.Ativo = 1
            ORDER BY
                UA.UsuarioAutenticacaoId;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<LoginUsuarioModel>(
                sql,
                new
                {
                    IdentificadorExterno =
                        identificadorExterno,

                    TipoAutenticacaoId =
                        TipoAutenticacaoConstants
                            .GovBr
                });
    }

    public async Task<string?>
        ObterSenhaHashAsync(
            int usuarioId)
    {
        const string sql = @"
            SELECT TOP 1
                SenhaHash
            FROM UsuarioAutenticacao
            WHERE UsuarioId = @UsuarioId
              AND TipoAutenticacaoId = @TipoAutenticacaoId
              AND Ativo = 1;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<string>(
                sql,
                new
                {
                    UsuarioId =
                        usuarioId,

                    TipoAutenticacaoId =
                        TipoAutenticacaoConstants.Local
                });
    }

    public async Task<bool> AtualizarSenhaAsync(
        int usuarioId,
        string senhaHash)
    {
        const string sql = @"
            UPDATE UsuarioAutenticacao
            SET
                SenhaHash = @SenhaHash,
                DataAlteracao = GETDATE(),
                TentativasFalhas = 0,
                BloqueadoAte = NULL
            WHERE UsuarioId = @UsuarioId
              AND TipoAutenticacaoId = @TipoAutenticacaoId
              AND Ativo = 1;";

        using var connection =
            _dbSession.CreateConnection();

        var linhas =
            await connection.ExecuteAsync(
                sql,
                new
                {
                    UsuarioId =
                        usuarioId,

                    SenhaHash =
                        senhaHash,

                    TipoAutenticacaoId =
                        TipoAutenticacaoConstants.Local
                });

        return linhas > 0;
    }
}
