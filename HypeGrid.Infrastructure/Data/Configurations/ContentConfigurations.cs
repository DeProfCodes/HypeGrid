using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Content;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Services");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.Category).HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.SortOrder);
    }
}

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> b)
    {
        b.ToTable("Packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(800);
        b.Property(x => x.PriceLabel).HasMaxLength(100);
        b.Property(x => x.Color).HasMaxLength(40);
        b.Property(x => x.Cta).HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.SortOrder);
    }
}

public sealed class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> b)
    {
        b.ToTable("Testimonials");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
        b.Property(x => x.BrandName).HasMaxLength(200);
        b.Property(x => x.RoleOrCategory).HasMaxLength(150);
        b.Property(x => x.Quote).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => x.SortOrder);
    }
}

public sealed class CaseStudyConfiguration : IEntityTypeConfiguration<CaseStudy>
{
    public void Configure(EntityTypeBuilder<CaseStudy> b)
    {
        b.ToTable("CaseStudies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Summary).HasMaxLength(600);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.Color).HasMaxLength(40);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.SortOrder);
    }
}

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.ToTable("SiteSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(120).IsRequired();
        b.Property(x => x.Group).HasMaxLength(60).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
        // Settings are keyed per group, so the same key (e.g. "support_email")
        // may legitimately appear under different groups. Uniqueness is on the
        // (Group, Key) pair, not Key alone.
        b.HasIndex(x => new { x.Group, x.Key }).IsUnique();
    }
}
