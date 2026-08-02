using System.Text.Json;
using Kiosk.Domain.Common;

namespace Kiosk.Api.Middleware;

public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = ex.Code,
                message = ex.Message
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "ERROR_INTERNO",
                message = "Ocurrió un error inesperado."
            }));
        }
    }
}
