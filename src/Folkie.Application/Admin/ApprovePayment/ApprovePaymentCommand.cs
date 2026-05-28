using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ApprovePayment;

public sealed record ApprovePaymentCommand(Guid PaymentId, string? Note) : IRequest<Unit>;

public sealed class ApprovePaymentHandler : IRequestHandler<ApprovePaymentCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApprovePaymentHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApprovePaymentCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == cmd.PaymentId, ct)
            ?? throw new NotFoundException("Ödeme");

        payment.Approve(user.Id, cmd.Note);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed record MarkTransferredCommand(Guid PaymentId, string TransferReference, string? Note)
    : IRequest<Unit>;

public sealed class MarkTransferredValidator : AbstractValidator<MarkTransferredCommand>
{
    public MarkTransferredValidator()
    {
        RuleFor(x => x.TransferReference).NotEmpty().MaximumLength(100);
    }
}

public sealed class MarkTransferredHandler : IRequestHandler<MarkTransferredCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkTransferredHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkTransferredCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == cmd.PaymentId, ct)
            ?? throw new NotFoundException("Ödeme");

        payment.MarkTransferred(cmd.TransferReference, cmd.Note);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
