using Folkie.Application.Admin.ApprovePayment;
using Folkie.Application.Admin.GetStats;
using Folkie.Application.Admin.GetUserDetail;
using Folkie.Application.Admin.ListApplications;
using Folkie.Application.Admin.ListBrandsForApproval;
using Folkie.Application.Admin.ListCampaigns;
using Folkie.Application.Admin.ListCreatorsForApproval;
using Folkie.Application.Admin.ListPayments;
using Folkie.Application.Admin.ListSubmissions;
using Folkie.Application.Admin.ListUsers;
using Folkie.Application.Admin.ModerateBrand;
using Folkie.Application.Admin.ModerateCreator;
using MediatR;

namespace Folkie.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        group.MapGet("/users", async (
            string? role,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListUsersQuery(role, page ?? 1, pageSize ?? 50), ct)))
            .WithName("AdminListUsers");

        group.MapGet("/payments", async (
            string? status,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListPaymentsQuery(status), ct)))
            .WithName("AdminListPayments");

        group.MapPost("/payments/{id:guid}/approve", async (
            Guid id,
            AdminNoteBody? body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ApprovePaymentCommand(id, body?.Note), ct);
            return Results.NoContent();
        })
        .WithName("AdminApprovePayment");

        group.MapPost("/payments/{id:guid}/transfer", async (
            Guid id,
            TransferBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new MarkTransferredCommand(id, body.TransferReference, body.Note), ct);
            return Results.NoContent();
        })
        .WithName("AdminMarkTransferred");

        group.MapGet("/campaigns", async (
            string? status,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(
                new ListCampaignsQuery(status, page ?? 1, pageSize ?? 50), ct)))
            .WithName("AdminListCampaigns");

        group.MapGet("/applications", async (
            string? status,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(
                new ListApplicationsQuery(status, page ?? 1, pageSize ?? 50), ct)))
            .WithName("AdminListApplications");

        group.MapGet("/submissions", async (
            string? status,
            int? page,
            int? pageSize,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(
                new ListSubmissionsQuery(status, page ?? 1, pageSize ?? 50), ct)))
            .WithName("AdminListSubmissions");

        group.MapGet("/stats", async (
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new GetStatsQuery(), ct)))
            .WithName("AdminGetStats");

        group.MapGet("/users/{id:guid}", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new GetUserDetailQuery(id), ct)))
            .WithName("AdminGetUserDetail");

        // ─── Moderation: brand approval queue ──────────────────────
        group.MapGet("/brands", async (
            string? filter,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListBrandsForApprovalQuery(filter ?? "pending"), ct)))
            .WithName("AdminListBrands");

        group.MapPost("/brands/{id:guid}/verify", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateBrandCommand(id, BrandModerationAction.Verify), ct);
            return Results.NoContent();
        })
        .WithName("AdminVerifyBrand");

        group.MapPost("/brands/{id:guid}/suspend", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateBrandCommand(id, BrandModerationAction.Suspend), ct);
            return Results.NoContent();
        })
        .WithName("AdminSuspendBrand");

        group.MapPost("/brands/{id:guid}/reactivate", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateBrandCommand(id, BrandModerationAction.Reactivate), ct);
            return Results.NoContent();
        })
        .WithName("AdminReactivateBrand");

        // ─── Moderation: creator approval queue ────────────────────
        group.MapGet("/creators", async (
            string? filter,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListCreatorsForApprovalQuery(filter ?? "pending"), ct)))
            .WithName("AdminListCreators");

        group.MapPost("/creators/{id:guid}/verify", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateCreatorCommand(id, CreatorModerationAction.Verify), ct);
            return Results.NoContent();
        })
        .WithName("AdminVerifyCreator");

        group.MapPost("/creators/{id:guid}/suspend", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateCreatorCommand(id, CreatorModerationAction.Suspend), ct);
            return Results.NoContent();
        })
        .WithName("AdminSuspendCreator");

        group.MapPost("/creators/{id:guid}/reactivate", async (
            Guid id,
            IMediator m,
            CancellationToken ct) =>
        {
            await m.Send(new ModerateCreatorCommand(id, CreatorModerationAction.Reactivate), ct);
            return Results.NoContent();
        })
        .WithName("AdminReactivateCreator");

        return app;
    }
}

public sealed record AdminNoteBody(string? Note);
public sealed record TransferBody(string TransferReference, string? Note);
