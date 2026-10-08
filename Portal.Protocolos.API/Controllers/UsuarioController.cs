using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Seguranca.GovBr;
using Portal.Protocolos.API.Models.Seguranca.Usuario;
using Portal.Protocolos.API.Service.Cadastros;
using Portal.Protocolos.API.Service.Seguranca;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsuarioController
    : ControllerBase
{
    private readonly UsuarioService
        _usuarioService;

    private readonly JwtService
        _jwtService;

    public UsuarioController(
        UsuarioService usuarioService,
        JwtService jwtService)
    {
        _usuarioService =
            usuarioService;

        _jwtService =
            jwtService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        UsuarioCadastroRequest request)
    {
        await _usuarioService
            .CriarAsync(
                request);

        return Ok(
            new
            {
                message =
                    "Usuário cadastrado com sucesso."
            });
    }

    [HttpPost("govbr/concluir")]
    public async Task<IActionResult>ConcluirCadastroGovBr(GovBrConcluirCadastroRequest request)
    {
        var usuario =
            await _usuarioService
                .ConcluirCadastroGovBrAsync(
                    request);

        var response =
            _jwtService
                .GerarToken(
                    usuario);

        return Ok(response);
    }

    [Authorize]
    [HttpPut("senha")]
    public async Task<IActionResult>
        AlterarSenha(
            AlterarSenhaRequest request)
    {
        var usuarioIdClaim =
            User.FindFirst(
                "UsuarioId")
                ?.Value;

        if (
            !int.TryParse(
                usuarioIdClaim,
                out var usuarioId))
        {
            return Unauthorized(
                new
                {
                    message =
                        "Usuário não identificado."
                });
        }

        await _usuarioService
            .AlterarSenhaAsync(
                usuarioId,
                request);

        return Ok(
            new
            {
                message =
                    "Senha alterada com sucesso."
            });
    }


    [Authorize]
    [HttpPut("meus-dados")]
    public async Task<IActionResult> AtualizarMeusDados(
    UsuarioMeusDadosAtualizarRequest request)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            usuarioId <= 0)
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        var dados = await _usuarioService
            .AtualizarMeusDadosAsync(
                usuarioId,
                request);

        return Ok(dados);
    }

    [Authorize]
    [HttpGet("meus-dados")]
    public async Task<IActionResult> ObterMeusDados()
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            usuarioId <= 0)
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        var dados = await _usuarioService
            .ObterMeusDadosAsync(usuarioId);

        return Ok(dados);
    }
}
