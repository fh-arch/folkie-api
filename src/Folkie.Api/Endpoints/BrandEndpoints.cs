using Folkie.Application.Brand.ApproveApplication;
using Folkie.Application.Brand.CreateCampaign;
using Folkie.Application.Brand.Favorites;
using Folkie.Application.Brand.GetCampaign;
using Folkie.Application.Brand.GetCreator;
using Folkie.Application.Brand.GetMyProfile;
using Folkie.Application.Brand.ListApplications;
using Folkie.Application.Brand.ListCampaigns;
using Folkie.Application.Brand.RejectApplication;
using Folkie.Application.Brand.SaveProfile;
using Folkie.Application.Brand.SearchCreators;
using Folkie.Application.Brand.SubmitCampaign;
using MediatR;

namespace Folkie.Api.Endpoints;

public static class BrandEndpoints
{
    public static IEndpointRouteBuilder MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/brand")
            .WithTags("Brand")
            .RequireAuthorization();

        // Profile
        group.MapGet("/profile", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetMyBrandProfileQuery(), ct)))
            .WithName("GetMyBrandProfile");

        group.MapPut("/profile", async (SaveBrandProfileCommand cmd, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(cmd, ct)))
            .WithName("SaveBrandProfile");

        // Campaigns
        group.MapPost("/campaigns", async (CreateCampaignCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var id = await m.Send(cmd, ct);
            return Results.Created($"/api/v1/brand/campaigns/{id}", new { id });
        })
        .WithName("CreateCampaign");

        group.MapGet("/campaigns", async (string? status, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListBrandCampaignsQuery(status), ct)))
            .WithName("ListBrandCampaigns");

        group.MapGet("/campaigns/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetBrandCampaignQuery(id), ct)))
            .WithName("GetBrandCampaign");

        group.MapPost("/campaigns/{id:guid}/submit", async (Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new SubmitCampaignCommand(id), ct)))
            .WithName("SubmitCampaign");

        group.MapGet("/campaigns/{id:guid}/applications", async (
            Guid id, string? status, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListCampaignApplicationsQuery(id, status), ct)))
            .WithName("ListCampaignApplications");

        // Applications
        group.MapPost("/applications/{id:guid}/approve", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new ApproveApplicationCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("ApproveApplication");

        group.MapPost("/applications/{id:guid}/reject", async (
            Guid id,
            RejectBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new RejectApplicationCommand(id, body.Reason), ct);
            return Results.NoContent();
        })
        .WithName("RejectApplication");

        // Creator detail
        group.MapGet("/creators/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetCreatorForBrandQuery(id), ct)))
            .WithName("GetCreatorForBrand");

        // Creator search
        group.MapGet("/creators", async (
            string? q,
            string? category,
            string? city,
            int? minFollowers,
            int? maxFollowers,
            decimal? minEngagement,
            string? tier,
            bool? verifiedOnly,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
        {
            var result = await m.Send(new SearchCreatorsQuery(
                q, category, city, minFollowers, maxFollowers, minEngagement,
                tier, verifiedOnly ?? false, page ?? 1, pageSize ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("SearchCreators");

        // Favorites
        group.MapGet("/favorites", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListFavoritesQuery(), ct)))
            .WithName("ListFavorites");

        group.MapPost("/favorites", async (
            AddFavoriteBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            var id = await m.Send(new AddFavoriteCommand(body.InfluencerProfileId, body.Note), ct);
            return Results.Created($"/api/v1/brand/favorites/{id}", new { id });
        })
        .WithName("AddFavorite");

        group.MapDelete("/favorites/{influencerId:guid}", async (
            Guid influencerId,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new RemoveFavoriteCommand(influencerId), ct);
            return Results.NoContent();
        })
        .WithName("RemoveFavorite");

        return app;
    }
}

public sealed record RejectBody(string Reason);
public sealed record AddFavoriteBody(Guid InfluencerProfileId, string? Note);
