using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;

namespace Portal.Protocolos.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/processos/{processoId:long}/anexos")]
    public class ProcessoAnexoController : ControllerBase
    {
        private readonly ProcessoAnexoService _service;

        private readonly ProcessoRepository
            _processoRepository;

        private readonly ProcessoCondutorRepository
            _processoCondutorRepository;

        public ProcessoAnexoController(
            ProcessoAnexoService service,
            ProcessoRepository processoRepository,
            ProcessoCondutorRepository processoCondutorRepository)
        {
            _service =
                service;

            _processoRepository =
                processoRepository;

            _processoCondutorRepository =
                processoCondutorRepository;
        }

        /*
         * =====================================================
         * AUTORIZAÇÃO PARA VISUALIZAÇÃO
         * =====================================================
         *
         * Pode visualizar:
         *
         * 1 - proprietário do processo WEB;
         *
         * 2 - solicitante do processo presencial
         *     identificado pelo CPF;
         *
         * 3 - condutor indicado que ainda possui
         *     indicação pendente.
         */
        private async Task<bool> PodeVisualizarAsync(
            long processoId)
        {
            var usuarioIdClaim =
                User.FindFirst("UsuarioId")?.Value;

            if (!int.TryParse(
                    usuarioIdClaim,
                    out var usuarioId))
            {
                return false;
            }

            var documento =
                User.FindFirst("Documento")?.Value;

            if (string.IsNullOrWhiteSpace(
                    documento))
            {
                return false;
            }

            /*
             * PRIMEIRO:
             *
             * WEB ou PRESENCIAL.
             */
            var podeVisualizarProcesso =
                await _processoRepository
                    .UsuarioPodeVisualizarProcessoAsync(
                        processoId,
                        usuarioId,
                        documento);

            if (podeVisualizarProcesso)
            {
                return true;
            }

            /*
             * SEGUNDO:
             *
             * Mantemos a regra que já existia
             * para o condutor indicado.
             */
            documento =
                SomenteNumeros(
                    documento);

            if (string.IsNullOrWhiteSpace(
                    documento))
            {
                return false;
            }

            return await _processoCondutorRepository
                .PossuiIndicacaoPendenteAsync(
                    processoId,
                    documento);
        }

        /*
         * =====================================================
         * INSERIR ANEXO
         * =====================================================
         *
         * Por segurança, continuamos permitindo
         * inclusão somente pelo dono WEB do processo.
         *
         * Processo presencial será apenas visualizado
         * pelo cidadão nesta tela.
         */
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Inserir(
            long processoId,
            [FromForm] ProcessoAnexoUploadRequest request)
        {
            var usuarioIdClaim =
                User.FindFirst("UsuarioId")?.Value;

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

            var pertenceAoUsuario =
                await _processoRepository
                    .PertenceAoUsuarioAsync(
                        processoId,
                        usuarioId);

            if (!pertenceAoUsuario)
            {
                return NotFound(new
                {
                    message =
                        "Processo não encontrado."
                });
            }

            var anexo =
                await _service
                    .InserirAsync(
                        processoId,
                        request.Arquivo,
                        request.TipoDocumento);

            return Ok(
                anexo);
        }

        /*
         * =====================================================
         * LISTAR ANEXOS
         * =====================================================
         */
        [HttpGet]
        public async Task<IActionResult> Listar(
            long processoId)
        {
            var podeVisualizar =
                await PodeVisualizarAsync(
                    processoId);

            if (!podeVisualizar)
            {
                return NotFound(new
                {
                    message =
                        "Processo não encontrado."
                });
            }

            var anexos =
                await _service
                    .ListarPorProcessoAsync(
                        processoId);

            return Ok(
                anexos);
        }

        /*
         * =====================================================
         * EXCLUIR
         * =====================================================
         *
         * Continua restrito ao processo WEB
         * pertencente ao usuário.
         */
        [HttpDelete("{processoAnexoId:long}")]
        public async Task<IActionResult> Excluir(
            long processoId,
            long processoAnexoId)
        {
            var usuarioIdClaim =
                User.FindFirst("UsuarioId")?.Value;

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

            var pertenceAoUsuario =
                await _processoRepository
                    .PertenceAoUsuarioAsync(
                        processoId,
                        usuarioId);

            if (!pertenceAoUsuario)
            {
                return NotFound(new
                {
                    message =
                        "Processo não encontrado."
                });
            }

            var anexo =
                await _service
                    .ObterAsync(
                        processoAnexoId);

            if (anexo.ProcessoId !=
                processoId)
            {
                return NotFound(new
                {
                    message =
                        "Anexo não encontrado."
                });
            }

            await _service
                .ExcluirAsync(
                    processoAnexoId);

            return Ok(new
            {
                message =
                    "Anexo removido com sucesso."
            });
        }

        /*
         * =====================================================
         * VISUALIZAR / BAIXAR ARQUIVO
         * =====================================================
         */
        [HttpGet("{processoAnexoId:long}/arquivo")]
        public async Task<IActionResult> ObterArquivo(
            long processoId,
            long processoAnexoId)
        {
            var podeVisualizar =
                await PodeVisualizarAsync(
                    processoId);

            if (!podeVisualizar)
            {
                return NotFound(new
                {
                    message =
                        "Processo não encontrado."
                });
            }

            var anexo =
                await _service
                    .ObterAsync(
                        processoAnexoId);

            if (anexo.ProcessoId !=
                processoId)
            {
                return NotFound(new
                {
                    message =
                        "Anexo não encontrado."
                });
            }

            var caminhoCompleto =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    anexo.CaminhoArquivo);

            if (!System.IO.File.Exists(
                    caminhoCompleto))
            {
                return NotFound(new
                {
                    message =
                        "Arquivo físico não encontrado."
                });
            }

            var bytes =
                await System.IO.File
                    .ReadAllBytesAsync(
                        caminhoCompleto);

            /*
             * Usa o ContentType salvo no banco,
             * quando disponível.
             *
             * Caso o seu ProcessoAnexoModel
             * não possua ContentType, mantenha
             * "application/pdf".
             */
            var contentType =
                ObterContentType(
                    anexo.NomeOriginal);

            return File(
                bytes,
                contentType,
                anexo.NomeOriginal);
        }

        private static string SomenteNumeros(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(
                    valor))
            {
                return string.Empty;
            }

            return new string(
                valor
                    .Where(char.IsDigit)
                    .ToArray());
        }

        private static string ObterContentType(
            string? nomeArquivo)
        {
            var extensao =
                Path.GetExtension(
                        nomeArquivo ?? string.Empty)
                    .ToLowerInvariant();

            return extensao switch
            {
                ".pdf" =>
                    "application/pdf",

                ".jpg" or ".jpeg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                ".webp" =>
                    "image/webp",

                _ =>
                    "application/octet-stream"
            };
        }
    }
}
