using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Portal.Protocolos.API.Models.Seguranca.Login;

namespace Portal.Protocolos.API.Service.Seguranca;

public sealed class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse GerarToken(LoginUsuarioModel usuario)
    {
        return CriarToken(usuario, Array.Empty<string>(), null);
    }

    public LoginResponse GerarTokenInterno(
        LoginUsuarioModel usuario,
        IReadOnlyList<string> perfis)
    {
        if (perfis.Count == 0 || perfis.Any(codigo => codigo is not
            ("ATENDENTE" or "ANALISTA" or "SUPERVISOR" or "ADMINISTRADOR")))
        {
            throw new ArgumentException("Perfis internos inválidos.", nameof(perfis));
        }

        return CriarToken(usuario, perfis, "INTERNO");
    }

    private LoginResponse CriarToken(
        LoginUsuarioModel usuario,
        IReadOnlyList<string> perfis,
        string? canal)
    {
        var chave = _configuration["Jwt:Key"]!;
        var emissor = _configuration["Jwt:Issuer"]!;
        var audiencia = _configuration["Jwt:Audience"]!;
        var expiracao = DateTime.UtcNow.AddHours(8);
        var documento = NormalizarDocumento(usuario.Documento);

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.UsuarioId.ToString()),

            new Claim(
                "UsuarioId",
                usuario.UsuarioId.ToString()),

            new Claim(
                "Documento",
                documento),

            new Claim(
                "Nome",
                usuario.Nome),

            new Claim(
                ClaimTypes.Email,
                usuario.Email),

            new Claim(
                "TipoPessoaId",
                usuario.TipoPessoaId.ToString())
        };

        if (usuario.TipoPessoaJuridicaId.HasValue)
        {
            claims.Add(
                new Claim(
                    "TipoPessoaJuridicaId",
                    usuario.TipoPessoaJuridicaId.Value.ToString()));
        }

        if (canal is not null)
        {
            claims.Add(new Claim("Canal", canal));
        }

        foreach (var perfil in perfis.Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, perfil));
        }

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chave));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: audiencia,
            claims: claims,
            expires: expiracao,
            signingCredentials: credentials);

        return new LoginResponse
        {
            Token =
                new JwtSecurityTokenHandler()
                    .WriteToken(token),

            ExpiraEm = expiracao,

            Nome = usuario.Nome,

            Documento = usuario.Documento,

            Email = usuario.Email,

            TipoPessoaId =
                usuario.TipoPessoaId,

            TipoPessoaJuridicaId =
                usuario.TipoPessoaJuridicaId
        };



    }

    private static string NormalizarDocumento(string documento)
    {
        var numeros = new string(
            documento
                .Where(char.IsDigit)
                .ToArray()
        );

        if (numeros.Length == 14 &&
            numeros.StartsWith("000"))
        {
            return numeros[^11..];
        }

        return numeros;
    }
}
