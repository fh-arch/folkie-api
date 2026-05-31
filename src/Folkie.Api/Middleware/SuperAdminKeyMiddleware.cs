namespace Folkie.Api.Middleware;

/// <summary>
/// If X-Super-Admin-Key header matches configured key, injects a synthetic
/// admin identity so the request bypasses Clerk JWT validation.
/// Key is stored in SuperAdmin:ApiKey config — never in code.
/// </summary>
public sealed class SuperAdminKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public SuperAdminKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config["SuperAdmin:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            var header = ctx.Request.Headers["X-Super-Admin-Key"].FirstOrDefault();
            if (header == _apiKey)
                ctx.Items["IsSuperAdmin"] = true;
        }
        await _next(ctx);
    }
}
