using Folkie.Application.Messaging;
using MediatR;

namespace Folkie.Api.Endpoints;

public static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/messaging")
            .WithTags("Messaging")
            .RequireAuthorization();

        group.MapGet("/conversations", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListConversationsQuery(), ct)))
            .WithName("ListConversations");

        group.MapGet("/conversations/{id:guid}/messages", async (
            Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListMessagesQuery(id), ct)))
            .WithName("ListMessages");

        group.MapPost("/conversations/{id:guid}/messages", async (
            Guid id,
            SendBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            var msgId = await m.Send(new SendMessageCommand(id, body.Body), ct);
            return Results.Created($"/api/v1/messaging/messages/{msgId}", new { id = msgId });
        })
        .WithName("SendMessage");

        group.MapPost("/conversations", async (
            StartConvBody body,
            IMediator m,
            CancellationToken ct) =>
        {
            var id = await m.Send(new StartConversationCommand(
                body.OtherUserId,
                body.CampaignId,
                body.Subject ?? "Direct Message",
                body.FirstMessage), ct);
            return Results.Created($"/api/v1/messaging/conversations/{id}", new { id });
        })
        .WithName("StartConversation");

        return app;
    }
}

public sealed record SendBody(string Body);
public sealed record StartConvBody(
    Guid OtherUserId,
    Guid? CampaignId,
    string? Subject,
    string? FirstMessage);
