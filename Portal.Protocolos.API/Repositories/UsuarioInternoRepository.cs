using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Protocolos.API.Models.Atendimento.Administracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Exceptions;
using System.Data;

namespace Portal.Protocolos.API.Repositories;

public sealed class UsuarioInternoRepository
{
    private static readonly string[] PerfisInternos =
    [
        "ATENDENTE",
        "ANALISTA",
        "SUPERVISOR",
        "ADMINISTRADOR"
    ];

    private readonly DbSession _dbSession;

    public UsuarioInternoRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<IReadOnlyList<UsuarioInternoResponse>> ListarAsync()
    {
        const string sql = """
            SELECT
                U.UsuarioId,
                U.Nome,
                U.Documento,
                U.Email,
                U.Celular,
                PInterno.Perfil,
                PInterno.Ativo,
                U.DataCadastro
            FROM dbo.Usuario U
            CROSS APPLY
            (
                SELECT TOP (1)
                    P.Codigo AS Perfil,
                    UP.Ativo
                FROM dbo.UsuarioPerfil UP
                INNER JOIN dbo.Perfil P
                    ON P.PerfilId = UP.PerfilId
                WHERE UP.UsuarioId = U.UsuarioId
                  AND P.Codigo IN
                      ('ATENDENTE', 'ANALISTA', 'SUPERVISOR', 'ADMINISTRADOR')
                ORDER BY
                    UP.Ativo DESC,
                    UP.DataCadastro DESC,
                    UP.UsuarioPerfilId DESC
            ) PInterno
            ORDER BY U.Nome, U.UsuarioId;
            """;

        using var connection = _dbSession.CreateConnection();

        var itens = await connection.QueryAsync<UsuarioInternoResponse>(sql);
        return itens.ToList();
    }

    public async Task<int> CriarOuVincularAsync(
        CriarUsuarioInternoRequest request,
        string senhaHash)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction =
            connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var ids = await ObterIdsCadastroAsync(
                request.Perfil,
                connection,
                transaction);

            var usuarioId = await connection.ExecuteScalarAsync<int?>(
                """
                SELECT UsuarioId
                FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
                WHERE Documento = @Documento;
                """,
                new { Documento = request.Cpf },
                transaction);

            await ValidarEmailDisponivelAsync(
                request.Email,
                usuarioId,
                connection,
                transaction);

            if (usuarioId.HasValue)
            {
                await connection.ExecuteAsync(
                    """
                    UPDATE dbo.Usuario
                    SET
                        TipoPessoaId = @TipoPessoaId,
                        StatusUsuarioId = @StatusUsuarioId,
                        Nome = @Nome,
                        Email = @Email,
                        Celular = @Celular,
                        DataAlteracao = SYSUTCDATETIME()
                    WHERE UsuarioId = @UsuarioId;
                    """,
                    new
                    {
                        UsuarioId = usuarioId.Value,
                        ids.TipoPessoaId,
                        ids.StatusUsuarioId,
                        request.Nome,
                        request.Email,
                        request.Celular
                    },
                    transaction);
            }
            else
            {
                usuarioId = await connection.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO dbo.Usuario
                    (
                        TipoPessoaId,
                        StatusUsuarioId,
                        Nome,
                        Documento,
                        Email,
                        Celular,
                        DataCadastro
                    )
                    OUTPUT INSERTED.UsuarioId
                    VALUES
                    (
                        @TipoPessoaId,
                        @StatusUsuarioId,
                        @Nome,
                        @Documento,
                        @Email,
                        @Celular,
                        SYSUTCDATETIME()
                    );
                    """,
                    new
                    {
                        ids.TipoPessoaId,
                        ids.StatusUsuarioId,
                        request.Nome,
                        Documento = request.Cpf,
                        request.Email,
                        request.Celular
                    },
                    transaction);
            }

            await DefinirAutenticacaoLocalAsync(
                usuarioId.Value,
                ids.TipoAutenticacaoId,
                senhaHash,
                connection,
                transaction);

            await DefinirPerfilInternoAsync(
                usuarioId.Value,
                ids.PerfilId,
                ativo: true,
                connection,
                transaction);

            transaction.Commit();
            return usuarioId.Value;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            throw new BusinessException(
                "CPF ou e-mail já cadastrado por outro usuário.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task AtualizarAsync(
        int usuarioId,
        AtualizarUsuarioInternoRequest request)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction =
            connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var existe = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
                WHERE UsuarioId = @UsuarioId;
                """,
                new { UsuarioId = usuarioId },
                transaction);

            if (existe == 0)
            {
                throw new BusinessException(
                    "Usuário interno não encontrado.");
            }

            var perfilId = await ObterPerfilIdAsync(
                request.Perfil,
                connection,
                transaction);

            await ValidarEmailDisponivelAsync(
                request.Email,
                usuarioId,
                connection,
                transaction);

            await connection.ExecuteAsync(
                """
                UPDATE dbo.Usuario
                SET
                    Nome = @Nome,
                    Email = @Email,
                    Celular = @Celular,
                    DataAlteracao = SYSUTCDATETIME()
                WHERE UsuarioId = @UsuarioId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    request.Nome,
                    request.Email,
                    request.Celular
                },
                transaction);

            await DefinirPerfilInternoAsync(
                usuarioId,
                perfilId,
                request.Ativo,
                connection,
                transaction);

            transaction.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            throw new BusinessException(
                "O e-mail informado já pertence a outro usuário.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task AlterarStatusAsync(
        int usuarioId,
        string perfil,
        bool ativo)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var perfilId = await ObterPerfilIdAsync(
                perfil,
                connection,
                transaction);

            var existe = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM dbo.UsuarioPerfil
                WHERE UsuarioId = @UsuarioId
                  AND PerfilId = @PerfilId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    PerfilId = perfilId
                },
                transaction);

            if (existe == 0)
            {
                throw new BusinessException(
                    "Perfil interno não encontrado para este usuário.");
            }

            if (ativo)
            {
                await DesativarPerfisInternosAsync(
                    usuarioId,
                    connection,
                    transaction);
            }

            await connection.ExecuteAsync(
                """
                UPDATE dbo.UsuarioPerfil
                SET
                    Ativo = @Ativo,
                    DataCadastro = CASE
                        WHEN @Ativo = 1 THEN SYSUTCDATETIME()
                        ELSE DataCadastro
                    END
                WHERE UsuarioId = @UsuarioId
                  AND PerfilId = @PerfilId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    PerfilId = perfilId,
                    Ativo = ativo
                },
                transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RedefinirSenhaAsync(
        int usuarioId,
        string senhaHash)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var tipoAutenticacaoId =
                await connection.ExecuteScalarAsync<short?>(
                    """
                    SELECT TipoAutenticacaoId
                    FROM dbo.TipoAutenticacao
                    WHERE Codigo = 'LOCAL'
                      AND Ativo = 1;
                    """,
                    transaction: transaction);

            if (!tipoAutenticacaoId.HasValue)
            {
                throw new BusinessException(
                    "Tipo de autenticação LOCAL não está configurado.");
            }

            var usuarioExiste = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM dbo.Usuario
                WHERE UsuarioId = @UsuarioId;
                """,
                new { UsuarioId = usuarioId },
                transaction);

            if (usuarioExiste == 0)
            {
                throw new BusinessException(
                    "Usuário interno não encontrado.");
            }

            await DefinirAutenticacaoLocalAsync(
                usuarioId,
                tipoAutenticacaoId.Value,
                senhaHash,
                connection,
                transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task<(
        short TipoPessoaId,
        short StatusUsuarioId,
        short TipoAutenticacaoId,
        short PerfilId)> ObterIdsCadastroAsync(
        string perfil,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var tipoPessoaId = await connection.ExecuteScalarAsync<short?>(
            """
            SELECT TipoPessoaId
            FROM dbo.TipoPessoa
            WHERE Codigo = 'FISICA'
              AND Ativo = 1;
            """,
            transaction: transaction);

        var statusUsuarioId = await connection.ExecuteScalarAsync<short?>(
            """
            SELECT StatusUsuarioId
            FROM dbo.StatusUsuario
            WHERE Codigo = 'ATIVO'
              AND Ativo = 1
              AND PermiteAutenticacao = 1;
            """,
            transaction: transaction);

        var tipoAutenticacaoId =
            await connection.ExecuteScalarAsync<short?>(
                """
                SELECT TipoAutenticacaoId
                FROM dbo.TipoAutenticacao
                WHERE Codigo = 'LOCAL'
                  AND Ativo = 1;
                """,
                transaction: transaction);

        var perfilId = await ObterPerfilIdAsync(
            perfil,
            connection,
            transaction);

        if (!tipoPessoaId.HasValue)
        {
            throw new BusinessException(
                "Tipo de pessoa FISICA não está configurado.");
        }

        if (!statusUsuarioId.HasValue)
        {
            throw new BusinessException(
                "Status de usuário ATIVO não está configurado.");
        }

        if (!tipoAutenticacaoId.HasValue)
        {
            throw new BusinessException(
                "Tipo de autenticação LOCAL não está configurado.");
        }

        return (
            tipoPessoaId.Value,
            statusUsuarioId.Value,
            tipoAutenticacaoId.Value,
            perfilId);
    }

    private static async Task<short> ObterPerfilIdAsync(
        string perfil,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var perfilId = await connection.ExecuteScalarAsync<short?>(
            """
            SELECT PerfilId
            FROM dbo.Perfil
            WHERE Codigo = @Perfil
              AND Ativo = 1;
            """,
            new { Perfil = perfil },
            transaction);

        if (!perfilId.HasValue)
        {
            throw new BusinessException(
                "Perfil interno não encontrado ou inativo.");
        }

        return perfilId.Value;
    }

    private static async Task ValidarEmailDisponivelAsync(
        string email,
        int? usuarioId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var quantidade = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
            WHERE LOWER(Email) = LOWER(@Email)
              AND (@UsuarioId IS NULL OR UsuarioId <> @UsuarioId);
            """,
            new
            {
                Email = email,
                UsuarioId = usuarioId
            },
            transaction);

        if (quantidade > 0)
        {
            throw new BusinessException(
                "O e-mail informado já pertence a outro usuário.");
        }
    }

    private static async Task DefinirAutenticacaoLocalAsync(
        int usuarioId,
        short tipoAutenticacaoId,
        string senhaHash,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var existe = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM dbo.UsuarioAutenticacao
            WHERE UsuarioId = @UsuarioId
              AND TipoAutenticacaoId = @TipoAutenticacaoId;
            """,
            new
            {
                UsuarioId = usuarioId,
                TipoAutenticacaoId = tipoAutenticacaoId
            },
            transaction);

        if (existe > 0)
        {
            await connection.ExecuteAsync(
                """
                UPDATE dbo.UsuarioAutenticacao
                SET
                    SenhaHash = @SenhaHash,
                    TentativasFalhas = 0,
                    BloqueadoAte = NULL,
                    DataAlteracao = SYSUTCDATETIME(),
                    Ativo = 1
                WHERE UsuarioId = @UsuarioId
                  AND TipoAutenticacaoId = @TipoAutenticacaoId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    TipoAutenticacaoId = tipoAutenticacaoId,
                    SenhaHash = senhaHash
                },
                transaction);
        }
        else
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.UsuarioAutenticacao
                (
                    UsuarioId,
                    TipoAutenticacaoId,
                    SenhaHash,
                    IdentificadorExterno,
                    TentativasFalhas,
                    BloqueadoAte,
                    UltimoAcesso,
                    DataCadastro,
                    DataAlteracao,
                    Ativo
                )
                VALUES
                (
                    @UsuarioId,
                    @TipoAutenticacaoId,
                    @SenhaHash,
                    NULL,
                    0,
                    NULL,
                    NULL,
                    SYSUTCDATETIME(),
                    NULL,
                    1
                );
                """,
                new
                {
                    UsuarioId = usuarioId,
                    TipoAutenticacaoId = tipoAutenticacaoId,
                    SenhaHash = senhaHash
                },
                transaction);
        }
    }

    private static async Task DefinirPerfilInternoAsync(
        int usuarioId,
        short perfilId,
        bool ativo,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        await DesativarPerfisInternosAsync(
            usuarioId,
            connection,
            transaction);

        var existe = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM dbo.UsuarioPerfil
            WHERE UsuarioId = @UsuarioId
              AND PerfilId = @PerfilId;
            """,
            new
            {
                UsuarioId = usuarioId,
                PerfilId = perfilId
            },
            transaction);

        if (existe > 0)
        {
            await connection.ExecuteAsync(
                """
                UPDATE dbo.UsuarioPerfil
                SET
                    Ativo = @Ativo,
                    DataCadastro = SYSUTCDATETIME()
                WHERE UsuarioId = @UsuarioId
                  AND PerfilId = @PerfilId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    PerfilId = perfilId,
                    Ativo = ativo
                },
                transaction);
        }
        else
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.UsuarioPerfil
                (
                    UsuarioId,
                    PerfilId,
                    DataCadastro,
                    Ativo
                )
                VALUES
                (
                    @UsuarioId,
                    @PerfilId,
                    SYSUTCDATETIME(),
                    @Ativo
                );
                """,
                new
                {
                    UsuarioId = usuarioId,
                    PerfilId = perfilId,
                    Ativo = ativo
                },
                transaction);
        }
    }

    private static Task DesativarPerfisInternosAsync(
        int usuarioId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        return connection.ExecuteAsync(
            """
            UPDATE UP
            SET UP.Ativo = 0
            FROM dbo.UsuarioPerfil UP
            INNER JOIN dbo.Perfil P
                ON P.PerfilId = UP.PerfilId
            WHERE UP.UsuarioId = @UsuarioId
              AND P.Codigo IN
                  ('ATENDENTE', 'ANALISTA', 'SUPERVISOR', 'ADMINISTRADOR');
            """,
            new { UsuarioId = usuarioId },
            transaction);
    }
}
