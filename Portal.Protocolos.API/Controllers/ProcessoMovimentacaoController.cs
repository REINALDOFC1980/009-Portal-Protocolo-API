using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;

namespace Portal.Protocolos.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/processos/{processoId:long}/movimentacoes")]
    public class ProcessoMovimentacaoController : ControllerBase
    {
        private readonly ProcessoMovimentacaoService _service;
        private readonly ProcessoRepository _processoRepository;

        public ProcessoMovimentacaoController(
            ProcessoMovimentacaoService service,
            ProcessoRepository processoRepository)
        {
            _service = service;
            _processoRepository = processoRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            long processoId)
        {
            var usuarioIdClaim =
                User.FindFirst("UsuarioId")?.Value;

            var documentoClaim =
                User.FindFirst("Documento")?.Value;

            if (!int.TryParse(
                    usuarioIdClaim,
                    out var usuarioId))
            {
                return Unauthorized(new
                {
                    message =
                        "Usuário não identificado."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    documentoClaim))
            {
                return Unauthorized(new
                {
                    message =
                        "Documento do usuário não identificado."
                });
            }

            /*
             * Pode visualizar quando:
             *
             * 1 - processo pertence ao usuário WEB
             *
             * OU
             *
             * 2 - processo é presencial e
             *     DocumentoSolicitante é o mesmo
             *     CPF do cidadão logado.
             */
            var podeVisualizar =
                await _processoRepository
                    .UsuarioPodeVisualizarProcessoAsync(
                        processoId,
                        usuarioId,
                        documentoClaim);

            if (!podeVisualizar)
            {
                return NotFound(new
                {
                    message =
                        "Processo não encontrado."
                });
            }

            var movimentacoes =
                await _service
                    .ListarPorProcessoAsync(
                        processoId);

            return Ok(
                movimentacoes);
        }
    }
}
