using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Solicitante;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/solicitantes")]
public sealed class SolicitantesAtendimentoController : ControllerBase
{
    private readonly SolicitanteAtendimentoService _service;

    public SolicitantesAtendimentoController(SolicitanteAtendimentoService service)
    {
        _service = service;
    }

    [HttpGet("{cpf}")]
    public async Task<IActionResult> Consultar(string cpf)
    {
        return Ok(await _service.ConsultarAsync(cpf));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarSolicitanteRequest request)
    {
        return Ok(await _service.CadastrarAsync(request));
    }
    [HttpPut("{cpf}")]
    public async Task<IActionResult> Atualizar(
        string cpf, [FromBody] CadastrarSolicitanteRequest request)
    {
        var usuario = await _service.AtualizarAsync(cpf, request);
        if (usuario is null)
            return NotFound(new { message = "Solicitante não encontrado." });
        return Ok(usuario);
    }
}
