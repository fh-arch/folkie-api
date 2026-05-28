using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListUsers;

public sealed record ListUsersQuery(string? Role, int Page = 1, int PageSize = 50)
    : IRequest<List<AdminUserDto>>;

public sealed record AdminUserDto(
    Guid Id,
    string ClerkUserId,
    string Email,
    string Role,
    string? FullName,
    bool HasProfile,
    DateTimeOffset CreatedAt);

public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, List<AdminUserDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListUsersHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminUserDto>> Handle(ListUsersQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(q.Role)
            && Enum.TryParse<UserRole>(q.Role, ignoreCase: true, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        var rows = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(u => new
            {
                u.Id,
                u.ClerkUserId,
                u.Email,
                u.Role,
                u.FullName,
                u.CreatedAt,
                HasInfluencerProfile = _db.InfluencerProfiles.Any(p => p.UserId == u.Id),
                HasBrandProfile = _db.BrandProfiles.Any(p => p.UserId == u.Id),
            })
            .ToListAsync(ct);

        return rows.Select(r => new AdminUserDto(
                r.Id,
                r.ClerkUserId,
                r.Email,
                r.Role.ToString().ToLower(),
                r.FullName,
                r.HasInfluencerProfile || r.HasBrandProfile,
                r.CreatedAt))
            .ToList();
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
