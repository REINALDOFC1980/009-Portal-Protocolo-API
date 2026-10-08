using Dapper;
using Portal.Protocolos.API.Models.Seguranca.Usuario;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class UsuarioRepository
{
    private readonly DbSession _dbSession;

    public UsuarioRepository(
        DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<bool> ExisteDocumentoAsync(
        string documento)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Usuario
            WHERE Documento = @Documento;";

        using var connection =
            _dbSession.CreateConnection();

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Documento = documento
                });

        return quantidade > 0;
    }

    public async Task<bool> ExisteEmailAsync(
        string email)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Usuario
            WHERE Email = @Email;";

        using var connection =
            _dbSession.CreateConnection();

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Email = email
                });

        return quantidade > 0;
    }

    public async Task<UsuarioModel?>
        ObterPorDocumentoAsync(
            string documento)
    {
        const string sql = @"
            SELECT
                UsuarioId,
                TipoPessoaId,
                TipoPessoaJuridicaId,
                StatusUsuarioId,
                Nome,
                NomeFantasia,
                Documento,
                Email,
                Celular,
                RegistroCnh,
                UfCnh,
                ModeloCnh,
                DataCadastro,
                DataAlteracao
            FROM Usuario
            WHERE Documento = @Documento;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<UsuarioModel>(
                sql,
                new
                {
                    Documento = documento
                });
    }

    public async Task<string?>
        ObterDocumentoPorIdAsync(
            int usuarioId)
    {
        const string sql = @"
            SELECT Documento
            FROM Usuario
            WHERE UsuarioId = @UsuarioId;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<string>(
                sql,
                new
                {
                    UsuarioId = usuarioId
                });
    }

    public async Task<bool> ExisteEmailOutroUsuarioAsync(
    string email,
    int usuarioId)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM Usuario
        WHERE Email = @Email
          AND UsuarioId <> @UsuarioId;";

        using var connection =
            _dbSession.CreateConnection();

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Email = email,
                    UsuarioId = usuarioId
                });

        return quantidade > 0;
    }

    public async Task<bool> AtualizarMeusDadosAsync(
        int usuarioId,
        UsuarioMeusDadosAtualizarRequest request)
    {
        const string sql = @"
        UPDATE Usuario
        SET
            Nome = @Nome,
            NomeFantasia = @NomeFantasia,
            TipoPessoaJuridicaId = @TipoPessoaJuridicaId,
            Email = @Email,
            Celular = @Celular,
            RegistroCnh = @RegistroCnh,
            UfCnh = @UfCnh,
            ModeloCnh = @ModeloCnh,
            DataAlteracao = GETDATE()
        WHERE UsuarioId = @UsuarioId;";

        using var connection =
            _dbSession.CreateConnection();

        var linhasAfetadas = await connection.ExecuteAsync(
            sql,
            new
            {
                UsuarioId = usuarioId,
                request.Nome,
                request.NomeFantasia,
                request.TipoPessoaJuridicaId,
                request.Email,
                request.Celular,
                request.RegistroCnh,
                request.UfCnh,
                request.ModeloCnh
            });

        return linhasAfetadas > 0;
    }

    public async Task<int> InserirAsync(
        UsuarioModel usuario)
    {
        const string sql = @"
            INSERT INTO Usuario
            (
                TipoPessoaId,
                TipoPessoaJuridicaId,
                StatusUsuarioId,
                Nome,
                NomeFantasia,
                Documento,
                Email,
                Celular,
                RegistroCnh,
                UfCnh,
                ModeloCnh
            )
            VALUES
            (
                @TipoPessoaId,
                @TipoPessoaJuridicaId,
                @StatusUsuarioId,
                @Nome,
                @NomeFantasia,
                @Documento,
                @Email,
                @Celular,
                @RegistroCnh,
                @UfCnh,
                @ModeloCnh
            );

            SELECT CAST(
                SCOPE_IDENTITY()
                AS INT);";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .ExecuteScalarAsync<int>(
                sql,
                usuario);
    }

    public async Task<UsuarioMeusDadosResponse?>
    ObterMeusDadosAsync(int usuarioId)
    {
        const string sql = @"
        SELECT
            TipoPessoaId,
            TipoPessoaJuridicaId,
            Documento,
            Nome,
            NomeFantasia,
            Email,
            Celular,
            RegistroCnh,
            UfCnh,
            ModeloCnh
        FROM Usuario
        WHERE UsuarioId = @UsuarioId;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<UsuarioMeusDadosResponse>(
                sql,
                new
                {
                    UsuarioId = usuarioId
                });
    }

}
