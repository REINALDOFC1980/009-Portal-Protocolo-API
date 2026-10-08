using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Protocolos.API.Models.Atendimento.Solicitante;
using Portal.Protocolos.Infrastructure.Database.Connection;
using Portal.Protocolos.Shared.Exceptions;
using System.Data;

namespace Portal.Protocolos.API.Repositories;

public sealed class SolicitanteAtendimentoRepository
{
    private readonly DbSession _dbSession;

    public SolicitanteAtendimentoRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<SolicitanteAtendimentoResponse?> ObterPorCpfAsync(string cpf)
    {
        const string sql = """
            SELECT UsuarioId, Documento AS Cpf, Nome, Email, Celular,
                   RegistroCnh, UfCnh, ModeloCnh
            FROM dbo.Usuario
            WHERE Documento = @Cpf AND TipoPessoaId = 1;
            """;

        using var connection = _dbSession.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SolicitanteAtendimentoResponse>(
            sql, new { Cpf = cpf });
    }

    public async Task<SolicitanteAtendimentoResponse?> AtualizarAsync(
        CadastrarSolicitanteRequest solicitante)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var emailExiste = await connection.ExecuteScalarAsync<int>("""
                SELECT COUNT(1)
                FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
                WHERE Email = @Email AND Documento <> @Cpf;
                """, new { solicitante.Email, solicitante.Cpf }, transaction);
            if (emailExiste > 0)
                throw new BusinessException("E-mail já cadastrado para outro usuário.");

            const string sql = """
                UPDATE dbo.Usuario
                SET Nome = @Nome, Email = @Email, Celular = @Celular,
                    RegistroCnh = @RegistroCnh, UfCnh = @UfCnh,
                    ModeloCnh = @ModeloCnh, DataAlteracao = SYSUTCDATETIME()
                OUTPUT INSERTED.UsuarioId, INSERTED.Documento AS Cpf,
                       INSERTED.Nome, INSERTED.Email, INSERTED.Celular,
                       INSERTED.RegistroCnh, INSERTED.UfCnh, INSERTED.ModeloCnh
                WHERE Documento = @Cpf AND TipoPessoaId = 1;
                """;
            var usuario = await connection.QuerySingleOrDefaultAsync<SolicitanteAtendimentoResponse>(
                sql, solicitante, transaction);
            transaction.Commit();
            return usuario;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            throw new BusinessException("CPF ou e-mail já cadastrado. Confira os dados.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<SolicitanteAtendimentoResponse> InserirAsync(
        CadastrarSolicitanteRequest solicitante)
    {
        using var connection = _dbSession.CreateConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var perfilId = await connection.ExecuteScalarAsync<short?>("""
                SELECT PerfilId FROM dbo.Perfil
                WHERE Codigo = 'CIDADAO' AND Ativo = 1;
                """, transaction: transaction);
            if (!perfilId.HasValue)
                throw new BusinessException("Perfil Cidadão não está configurado.");

            var statusId = await connection.ExecuteScalarAsync<short?>("""
                SELECT StatusUsuarioId FROM dbo.StatusUsuario
                WHERE Codigo = 'PENDENTE_CONFIRMACAO' AND Ativo = 1;
                """, transaction: transaction);
            if (!statusId.HasValue)
                throw new BusinessException("Status Pendente de Confirmação não está configurado.");

            var cpfExiste = await connection.ExecuteScalarAsync<int>("""
                SELECT COUNT(1) FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
                WHERE Documento = @Cpf;
                """, new { solicitante.Cpf }, transaction);
            if (cpfExiste > 0)
                throw new BusinessException("CPF já cadastrado. Consulte novamente.");

            var emailExiste = await connection.ExecuteScalarAsync<int>("""
                SELECT COUNT(1) FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
                WHERE Email = @Email;
                """, new { solicitante.Email }, transaction);
            if (emailExiste > 0)
                throw new BusinessException("E-mail já cadastrado. Confira os dados do solicitante.");

            const string inserirUsuario = """
                INSERT INTO dbo.Usuario
                    (TipoPessoaId, StatusUsuarioId, Nome, Documento, Email,
                     Celular, RegistroCnh, UfCnh, ModeloCnh)
                OUTPUT INSERTED.UsuarioId
                VALUES
                    (1, @StatusId, @Nome, @Cpf, @Email, @Celular,
                     @RegistroCnh, @UfCnh, @ModeloCnh);
                """;
            var usuarioId = await connection.ExecuteScalarAsync<int>(inserirUsuario, new
            {
                StatusId = statusId.Value,
                solicitante.Nome,
                solicitante.Cpf,
                solicitante.Email,
                solicitante.Celular,
                solicitante.RegistroCnh,
                solicitante.UfCnh,
                solicitante.ModeloCnh
            }, transaction);

            await connection.ExecuteAsync("""
                INSERT INTO dbo.UsuarioPerfil (UsuarioId, PerfilId, Ativo)
                VALUES (@UsuarioId, @PerfilId, 1);
                """, new { UsuarioId = usuarioId, PerfilId = perfilId.Value }, transaction);

            transaction.Commit();
            return new SolicitanteAtendimentoResponse
            {
                UsuarioId = usuarioId,
                Cpf = solicitante.Cpf,
                Nome = solicitante.Nome,
                Email = solicitante.Email,
                Celular = solicitante.Celular,
                RegistroCnh = solicitante.RegistroCnh,
                UfCnh = solicitante.UfCnh,
                ModeloCnh = solicitante.ModeloCnh
            };
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            throw new BusinessException("CPF ou e-mail já cadastrado. Consulte novamente.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
