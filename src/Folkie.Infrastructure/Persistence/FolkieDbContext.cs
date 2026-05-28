using Folkie.Application.Common.Interfaces;
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

namespace Folkie.Infrastructure.Persistence;

public class FolkieDbContext : DbContext, IFolkieDbContext
{
    public FolkieDbContext(DbContextOptions<FolkieDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<InfluencerProfile> InfluencerProfiles => Set<InfluencerProfile>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<BrandFavorite> BrandFavorites => Set<BrandFavorite>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignApplication> CampaignApplications => Set<CampaignApplication>();
    public DbSet<ContentSubmission> ContentSubmissions => Set<ContentSubmission>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BrandPayment> BrandPayments => Set<BrandPayment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration'ları aynı assembly'den otomatik uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FolkieDbContext).Assembly);

        // Tablo ve kolon isimleri snake_case (PostgreSQL konvansiyonu)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
            foreach (var key in entity.GetKeys())
                key.SetName(ToSnakeCase(key.GetName()!));
            foreach (var foreignKey in entity.GetForeignKeys())
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length + 8);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
