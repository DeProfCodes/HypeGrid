using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Leads;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class EnquiryConfiguration : IEntityTypeConfiguration<Enquiry>
{
    public void Configure(EntityTypeBuilder<Enquiry> b)
    {
        b.ToTable("Enquiries");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Subject).HasMaxLength(300);
        b.Property(x => x.InterestType).HasMaxLength(100);
        b.Property(x => x.SourcePage).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
    }
}

public sealed class CampaignRequestConfiguration : IEntityTypeConfiguration<CampaignRequest>
{
    public void Configure(EntityTypeBuilder<CampaignRequest> b)
    {
        b.ToTable("CampaignRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.BrandName).HasMaxLength(200);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.CampaignType).HasMaxLength(100);
        b.Property(x => x.BudgetRange).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        // EF Core 8 maps List<string> as a JSON primitive collection column.
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
    }
}

public sealed class CreatorApplicationConfiguration : IEntityTypeConfiguration<CreatorApplication>
{
    public void Configure(EntityTypeBuilder<CreatorApplication> b)
    {
        b.ToTable("CreatorApplications");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.Province).HasMaxLength(120);
        b.Property(x => x.MainPlatform).HasMaxLength(50);
        b.Property(x => x.HandleOrProfileLink).HasMaxLength(300);
        b.Property(x => x.FollowerRange).HasMaxLength(50);
        b.Property(x => x.ContentNiche).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
    }
}

public sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> b)
    {
        b.ToTable("NewsletterSubscribers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200);
        b.Property(x => x.SourcePage).HasMaxLength(100);
        b.HasIndex(x => x.Email).IsUnique();
    }
}
