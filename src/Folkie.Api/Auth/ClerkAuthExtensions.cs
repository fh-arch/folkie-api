using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Folkie.Api.Auth;

public static class ClerkAuthExtensions
{
    /// <summary>
    /// Clerk JWT'sini doğrulayan JwtBearer setup'ı.
    /// Clerk'in JWKS endpoint'i Authority'den otomatik çekilir.
    /// JWT template (folkie-api) içinde role claim'i olduğu varsayılır.
    /// </summary>
    public static IServiceCollection AddClerkAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Clerk:Issuer"]
            ?? throw new InvalidOperationException("Clerk:Issuer eksik (örn: https://xxx.clerk.accounts.dev)");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = issuer;
                options.RequireHttpsMetadata = !string.IsNullOrEmpty(configuration["Clerk:AllowHttp"]) ? false : true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false, // Clerk varsayılan token'da aud yok
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Folkie.ClerkAuth");
                        if (ctx.Exception is Microsoft.IdentityModel.Tokens.SecurityTokenInvalidIssuerException ex)
                        {
                            logger.LogError(
                                "Clerk JWT issuer mismatch! Token issuer: '{Actual}' | Expected: '{Expected}'. " +
                                "Update CLERK_AUTHORITY in VPS .env.",
                                ex.InvalidIssuer, issuer);
                        }
                        else
                        {
                            logger.LogError("Clerk JWT validation failed: {Error}", ctx.Exception.Message);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireBrand",     p => p.RequireClaim("folkie_role", "Brand", "brand"));
            options.AddPolicy("RequireInfluencer", p => p.RequireClaim("folkie_role", "Influencer", "influencer"));
            options.AddPolicy("RequireAdmin",     p => p.RequireClaim("folkie_role", "Admin", "admin"));
        });

        return services;
    }
}
