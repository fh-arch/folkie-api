using Folkie.Application.Notifications;
using MediatR;

namespace Folkie.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
            bool? unreadOnly,
            int? limit,
            IMediator m,
            CancellationToken ct) =>
            Results.Ok(await m.Send(new ListNotificationsQuery(unreadOnly ?? false, limit ?? 50), ct)))
            .WithName("ListNotifications");

        group.MapPost("/{id:guid}/read", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new MarkNotificationReadCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead");

        group.MapPost("/read-all", async (IMediator m, CancellationToken ct) =>
        {
            await m.Send(new MarkAllReadCommand(), ct);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead");

        return app;
    }
}
