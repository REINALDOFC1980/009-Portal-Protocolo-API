using System.Net;
using System.Text.Json;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(
                ex,
                "Erro de negócio em {Metodo} {Caminho}",
                context.Request.Method,
                context.Request.Path);

            await EscreverRespostaAsync(
                context,
                HttpStatusCode.BadRequest,
                ex.Message);
        }
        catch (SitranApiException ex)
        {
            _logger.LogError(
                ex,
                "Erro de integração com o SITRAN em {Metodo} {Caminho}. Código: {Codigo}",
                context.Request.Method,
                context.Request.Path,
                ex.Codigo);

            await EscreverRespostaSitranAsync(
                context,
                ex.StatusCode,
                ex.Codigo,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro não tratado em {Metodo} {Caminho}",
                context.Request.Method,
                context.Request.Path);

            await EscreverRespostaAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno no servidor.");
        }
    }

    private static async Task EscreverRespostaAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string mensagem)
    {
        context.Response.StatusCode =
            (int)statusCode;

        context.Response.ContentType =
            "application/json";

        var resposta = new
        {
            message = mensagem
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(resposta));
    }

    private static async Task EscreverRespostaSitranAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string? codigo,
        string mensagem)
    {
        context.Response.StatusCode =
            (int)statusCode;

        context.Response.ContentType =
            "application/json";

        var resposta = new
        {
            codigo,
            message = mensagem
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(resposta));
    }
}
