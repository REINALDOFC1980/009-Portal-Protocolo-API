using Portal.Protocolos.API.Models.Seguranca.Login;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Portal.Protocolos.API.Service.Seguranca;

public sealed class LoginService
{
    private readonly UsuarioAutenticacaoRepository
        _usuarioAutenticacaoRepository;

    private readonly UsuarioPerfilRepository _usuarioPerfilRepository;

    public LoginService(
        UsuarioAutenticacaoRepository usuarioAutenticacaoRepository,
        UsuarioPerfilRepository usuarioPerfilRepository)
    {
        _usuarioAutenticacaoRepository =
            usuarioAutenticacaoRepository;
        _usuarioPerfilRepository = usuarioPerfilRepository;
    }

    public async Task<LoginUsuarioModel> AutenticarAsync(LoginRequest request)
    {
        var documento = SomenteNumeros(request.Documento);

        var usuario = await _usuarioAutenticacaoRepository.ObterLoginAsync(documento);

        if (usuario is null)
        {
            throw new BusinessException("CPF/CNPJ ou senha inválidos.");
        }

        if (!usuario.Ativo)
        {
            throw new BusinessException("Esta forma de autenticação está inativa.");
        }



        var senhaValida = ValidarSenha(request.Senha, usuario.SenhaHash);
        if (!senhaValida)
        {
            throw new BusinessException("CPF/CNPJ ou senha inválidos.");
        }

        return usuario;
    }


    public async Task<(LoginUsuarioModel Usuario, IReadOnlyList<string> Perfis)>
        AutenticarInternoAsync(LoginRequest request)
    {
        var usuario = await AutenticarAsync(request);
        var perfisAtivos = await _usuarioPerfilRepository
            .ListarCodigosAtivosAsync(usuario.UsuarioId);

        var perfisInternos = perfisAtivos
            .Where(codigo => codigo is
                "ATENDENTE" or "ANALISTA" or "SUPERVISOR" or "ADMINISTRADOR")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (perfisInternos.Length == 0)
        {
            throw new BusinessException("Usuário sem acesso ao atendimento presencial.");
        }

        return (usuario, perfisInternos);
    }

    private static string GerarMd5(string senha)
    {
        var bytes =
            MD5.HashData(
                Encoding.UTF8.GetBytes(senha));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant();
    }
    private static string SomenteNumeros(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return new string(valor.Where(char.IsDigit).ToArray());
    }

    private static bool ValidarSenha(string senha, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            return false;
        }

        if (senhaHash.StartsWith("$2a$") ||
            senhaHash.StartsWith("$2b$") ||
            senhaHash.StartsWith("$2y$"))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(
                    senha,
                    senhaHash);
            }
            catch
            {
                return false;
            }
        }

        var senhaMd5 = GerarMd5(senha);

        return senhaMd5.Equals(
            senhaHash,
            StringComparison.OrdinalIgnoreCase);
    }
}
