using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HypeGrid.Domain.Alerts;
using HypeGrid.Domain.Analytics;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Clients;
using HypeGrid.Domain.Common;
using HypeGrid.Domain.Content;
using HypeGrid.Domain.Creators;
using HypeGrid.Domain.Finance;
using HypeGrid.Domain.Identity;
using HypeGrid.Domain.Leads;
using HypeGrid.Domain.Marketing;

namespace HypeGrid.Infrastructure.Data;

/// <summary>
/// Primary EF Core database context for HypeGrid. Backs the public website
/// content + leads and the admin dashboard. Identity tables are remapped to
/// clean names; entity configurations are discovered from this assembly.
/// </summary>
public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Identity-adjacent
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Leads
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<CampaignRequest> CampaignRequests => Set<CampaignRequest>();
    public DbSet<CreatorApplication> CreatorApplications => Set<CreatorApplication>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    // Alerts / Deals Club (consent-based audience opt-in)
    public DbSet<AlertSubscriber> AlertSubscribers => Set<AlertSubscriber>();

    // Clients & creators
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Creator> Creators => Set<Creator>();

    // Campaigns
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignTask> Tasks => Set<CampaignTask>();
    public DbSet<Deliverable> Deliverables => Set<Deliverable>();
    public DbSet<Note> Notes => Set<Note>();

    // Finance
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payout> Payouts => Set<Payout>();

    // Content / CMS
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<CaseStudy> CaseStudies => Set<CaseStudy>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    // Marketing / advertising inventory
    public DbSet<HeroPlacement> HeroPlacements => Set<HeroPlacement>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<FeaturedVideo> FeaturedVideos => Set<FeaturedVideo>();

    // Analytics
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(500);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Marketing / analytics indexes (scalar entities — no separate config files).
        builder.Entity<Deal>(e =>
        {
            e.HasIndex(x => x.Slug);
            e.Property(x => x.OriginalPrice).HasPrecision(18, 2);
            e.Property(x => x.DealPrice).HasPrecision(18, 2);
        });
        builder.Entity<HeroPlacement>().HasIndex(x => new { x.IsActive, x.Priority });
        builder.Entity<AnalyticsEvent>().HasIndex(x => new { x.EntityType, x.EntityId, x.EventType });

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Central audit-timestamp stamping for every <see cref="BaseEntity"/>:
    /// sets <c>CreatedDate</c> once on insert and <c>UpdatedDate</c> on every
    /// update, so individual services never hand-set timestamps.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedDate == default)
                        entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = now;
                    break;
            }
        }
    }
}
