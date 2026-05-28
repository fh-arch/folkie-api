using Folkie.Application.Brand.ListCampaignSubmissions;
using Folkie.Application.Brand.ReviewSubmission;
using Folkie.Application.Creator.CreateSubmission;
using Folkie.Application.Creator.ListMySubmissions;
using Folkie.Application.Creator.PublishSubmission;
using MediatR;

namespace Folkie.Api.Endpoints;

public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        // Creator
        var creator = app.MapGroup("/api/v1/creator")
            .WithTags("Submissions")
            .RequireAuthorization();

        creator.MapGet("/submissions", async (
            string? status,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListMySubmissionsQuery(status), ct)))
            .WithName("ListMySubmissions");

        creator.MapPost("/submissions", async (
            CreateSubmissionCommand cmd,
            IMediator m,
            CancellationToken ct) =>
        {
            var id = await m.Send(cmd, ct);
            return Results.Created($"/api/v1/creator/submissions/{id}", new { id });
        })
        .WithName("CreateSubmission");

        creator.MapPost("/submissions/{id:guid}/publish", async (
            Guid id,
            PublishBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new PublishSubmissionCommand(id, body.TiktokUrl), ct);
            return Results.NoContent();
        })
        .WithName("PublishSubmission");

        // Brand
        var brand = app.MapGroup("/api/v1/brand")
            .WithTags("Submissions")
            .RequireAuthorization();

        brand.MapGet("/campaigns/{id:guid}/submissions", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListCampaignSubmissionsQuery(id), ct)))
            .WithName("ListCampaignSubmissions");

        brand.MapPost("/submissions/{id:guid}/approve", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ApproveSubmissionCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("ApproveSubmission");

        brand.MapPost("/submissions/{id:guid}/request-revision", async (
            Guid id,
            RevisionBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new RequestRevisionCommand(id, body.Note), ct);
            return Results.NoContent();
        })
        .WithName("RequestRevision");

        return app;
    }
}

public sealed record PublishBody(string TiktokUrl);
public sealed record RevisionBody(string Note);
