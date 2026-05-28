using Folkie.Api.Auth;
using Folkie.Api.Endpoints;
using Folkie.Api.Endpoints.Me;
using Folkie.Api.Middleware;
using Folkie.Api.Webhooks;
using Folkie.Application;
using Folkie.Application.Common.Interfaces;
using Folkie.Infrastructure;
using Folkie.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ─────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Conditional(
        _ => !string.IsNullOrEmpty(ctx.Configuration["Serilog:SeqUrl"]),
        wt => wt.Seq(ctx.Configuration["Serilog:SeqUrl"]!)));

// ─── DI ──────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSignalR();

// Clerk JWT auth
builder.Services.AddClerkAuth(builder.Configuration);

// CORS — frontend (Vercel + localhost)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
    options.AddPolicy("Web", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// Hangfire — DB hazır olunca aktif et (ConnectionStrings:Hangfire varsa)
var hangfireConn = builder.Configuration.GetConnectionString("Hangfire");
var enableHangfire = !string.IsNullOrEmpty(hangfireConn);
if (enableHangfire)
{
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(hangfireConn!)));
    builder.Services.AddHangfireServer();
}

// API + Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Folkie API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Clerk JWT (Bearer <token>)",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new()
    {
        [new()
        {
            Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" },
        }] = Array.Empty<string>(),
    });
});

var app = builder.Build();

// ─── DB migrations on startup ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FolkieDbContext>();
    db.Database.Migrate();
}

// ─── Pipeline ────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Web");
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/", () => Results.Ok(new
{
    service = "Folkie API",
    version = "0.1.0",
    time = DateTimeOffset.UtcNow,
}))
.AllowAnonymous()
.WithTags("Health");

app.MapGet("/healthz", async (FolkieDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy", db = "ok" })
        : Results.StatusCode(503);
})
.AllowAnonymous()
.WithTags("Health");

// Endpoints
app.MapMe();
app.MapClerkWebhook();
app.MapCreatorEndpoints();
app.MapBrandEndpoints();
app.MapSubmissionEndpoints();
app.MapAdminEndpoints();
app.MapNotificationEndpoints();
app.MapMessagingEndpoints();
app.MapFileEndpoints();
app.MapHub<Folkie.Api.Hubs.MessagingHub>("/hubs/messaging");

// Hangfire dashboard — sadece admin
if (enableHangfire)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAdminAuthorizationFilter() },
    });

    // Register recurring jobs after app is built
    Folkie.Infrastructure.BackgroundJobs.JobScheduler.RegisterRecurringJobs(app.Services);
}

app.Run();

/// <summary>Hangfire dashboard'a sadece admin role'ündeki kullanıcı erişebilir.</summary>
internal sealed class HangfireAdminAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = ((Hangfire.Dashboard.AspNetCoreDashboardContext)context).HttpContext;
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.HasClaim(c =>
                c.Type == "folkie_role"
                && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));
    }
}
