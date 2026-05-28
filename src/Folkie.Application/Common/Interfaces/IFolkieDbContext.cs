using Folkie.Domain.Applications;
using Folkie.Domain.Brands;
using Folkie.Domain.Campaigns;
using Folkie.Domain.Influencers;
using Folkie.Domain.Messaging;
using Folkie.Domain.Notifications;
using Folkie.Domain.Payments;
using Folkie.Domain.Reviews;
using Folkie.Domain.Submissions;
using Folkie.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Common.Interfaces;

public interface IFolkieDbContext
{
    DbSet<User> Users { get; }
    DbSet<InfluencerProfile> InfluencerProfiles { get; }
    DbSet<BrandProfile> BrandProfiles { get; }
    DbSet<BrandFavorite> BrandFavorites { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignApplication> CampaignApplications { get; }
    DbSet<ContentSubmission> ContentSubmissions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<BrandPayment> BrandPayments { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
