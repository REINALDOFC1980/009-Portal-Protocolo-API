using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Administracao;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.Shared.Exceptions;
using System.Security.Claims;

namespace Portal.Protocolos.API.Controllers.Administracao;

[ApiController]
[Route("api/administracao/usuarios-internos")]
[Authorize(
    Policy = "AtendimentoInterno",
    Roles = "ADMINISTRADOR")]
public sealed class UsuariosInternosController : ControllerBase
{
    private readonly UsuarioInternoService _service;

    public UsuariosInternosController(
        UsuarioInternoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await _service.ListarAsync();
        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        CriarUsuarioInternoRequest request)
    {
        var usuarioId =
            await _service.CriarAsync(request);

        return Ok(new
        {
            usuarioId,
            message = "Usuário interno cadastrado com sucesso."
        });
    }

    [HttpPut("{usuarioId:int}")]
    public async Task<IActionResult> Atualizar(
        int usuarioId,
        AtualizarUsuarioInternoRequest request)
    {
        await _service.AtualizarAsync(
            usuarioId,
            ObterUsuarioAtualId(),
            request);

        return Ok(new
        {
            usuarioId,
            message = "Usuário interno atualizado com sucesso."
        });
    }

    [HttpPatch("{usuarioId:int}/status")]
    public async Task<IActionResult> AlterarStatus(
        int usuarioId,
        AlterarStatusUsuarioInternoRequest request)
    {
        await _service.AlterarStatusAsync(
            usuarioId,
            ObterUsuarioAtualId(),
            request);

        return Ok(new
        {
            usuarioId,
            message = request.Ativo
                ? "Acesso interno ativado."
                : "Acesso interno inativado."
        });
    }

    [HttpPost("{usuarioId:int}/redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(
        int usuarioId,
        RedefinirSenhaUsuarioInternoRequest request)
    {
        await _service.RedefinirSenhaAsync(
            usuarioId,
            request);

        return Ok(new
        {
            usuarioId,
            message = "Senha redefinida com sucesso."
        });
    }

    private int ObterUsuarioAtualId()
    {
        var valor =
            User.FindFirstValue("UsuarioId") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(valor, out var usuarioId))
        {
            throw new BusinessException(
                "Usuário autenticado inválido.");
        }

        return usuarioId;
    }
}
