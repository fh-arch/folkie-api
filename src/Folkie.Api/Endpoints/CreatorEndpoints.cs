using Folkie.Application.Creator.ApplyCampaign;
using Folkie.Application.Creator.ConnectTiktok;
using Folkie.Application.Creator.DiscoverCampaigns;
using Folkie.Application.Creator.GetCampaign;
using Folkie.Application.Creator.GetMyProfile;
using Folkie.Application.Creator.ListApplications;
using Folkie.Application.Creator.SaveProfile;
using Folkie.Application.Creator.WithdrawApplication;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Folkie.Api.Endpoints;

public static class CreatorEndpoints
{
    public static IEndpointRouteBuilder MapCreatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/creator")
            .WithTags("Creator")
            .RequireAuthorization();

        // Profile
        group.MapGet("/profile", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetMyCreatorProfileQuery(), ct)))
            .WithName("GetMyCreatorProfile");

        // Badge (Folkie Stars)
        group.MapGet("/badge", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new Folkie.Application.Creator.Badge.GetMyBadgeQuery(), ct)))
            .WithName("GetMyBadge");

        group.MapPut("/profile", async (SaveCreatorProfileCommand cmd, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(cmd, ct)))
            .WithName("SaveCreatorProfile");

        // Campaign discovery
        group.MapGet("/campaigns", async (
            string? category,
            string? city,
            int? maxFollowers,
            bool? nanoOnly,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
        {
            var result = await m.Send(new DiscoverCampaignsQuery(
                category, city, maxFollowers, nanoOnly ?? false,
                page ?? 1, pageSize ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("DiscoverCampaigns");

        group.MapGet("/campaigns/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetCreatorCampaignQuery(id), ct)))
            .WithName("GetCreatorCampaign");

        // Applications
        group.MapPost("/campaigns/{id:guid}/apply", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var applicationId = await m.Send(new ApplyCampaignCommand(id), ct);
            return Results.Created($"/api/v1/creator/applications/{applicationId}", new { id = applicationId });
        })
        .WithName("ApplyCampaign");

        group.MapGet("/applications", async (string? status, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListMyApplicationsQuery(status), ct)))
            .WithName("ListMyApplications");

        group.MapDelete("/applications/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new WithdrawApplicationCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("WithdrawApplication");

        // TikTok OAuth
        group.MapPost("/tiktok/connect", async (
            ConnectTiktokRequest req,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ConnectTiktokCommand(req.Code, req.RedirectUri), ct);
            return Results.Ok(new { connected = true });
        })
        .WithName("ConnectTiktok");

        // TikTok Sandbox (demo / screen-record mode) — only active when TikTok:Sandbox=true
        group.MapPost("/tiktok/sandbox-connect", async (
            SandboxConnectRequest req,
            IMediator m,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (config["TikTok:Sandbox"] != "true")
                return Results.Forbid();
            await m.Send(new SandboxConnectTiktokCommand(req.MockHandle, req.MockFollowers, req.MockEngagement), ct);
            return Results.Ok(new { connected = true });
        })
        .WithName("SandboxConnectTiktok");

        return app;
    }
}

public sealed record ConnectTiktokRequest(string Code, string RedirectUri);
public sealed record SandboxConnectRequest(string MockHandle, int MockFollowers, decimal MockEngagement);
