using FluentValidation;
using Folkie.Application.Common.Exceptions;

namespace Folkie.Api.Middleware;

/// <summary>
/// Domain ve Application exception'larını HTTP yanıtlarına çevirir.
/// </summary>
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, message, details) = ex switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Geçersiz veri.",
                (object?)ve.Errors.Select(e => new { propertyName = e.PropertyName, errorMessage = e.ErrorMessage })),
            NotFoundException nfe => (StatusCodes.Status404NotFound, "not_found", nfe.Message, null),
            ForbiddenException fe => (StatusCodes.Status403Forbidden, "forbidden", fe.Message, null),
            ConflictException ce => (StatusCodes.Status409Conflict, "conflict", ce.Message, null),
            UnauthorizedAccessException ue => (StatusCodes.Status401Unauthorized, "unauthorized", ue.Message, null),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "bad_request", ae.Message, null),
            InvalidOperationException ioe => (StatusCodes.Status409Conflict, "invalid_state", ioe.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "Beklenmeyen bir hata oluştu.", null),
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Code}", code);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            type = code,
            title = message,
            status,
            detail = details,
        });
    }
}
