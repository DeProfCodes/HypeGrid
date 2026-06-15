using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Campaigns;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> b)
    {
        b.ToTable("Campaigns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(200);
        b.Property(x => x.Type).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.Phase).HasMaxLength(60).IsRequired();
        b.Property(x => x.Manager).HasMaxLength(150);
        b.Property(x => x.TargetAudience).HasMaxLength(400);
        b.Property(x => x.Budget).HasColumnType("decimal(18,2)");
        // Platforms (List<string>) → JSON primitive collection (EF Core 8).
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ClientId);
    }
}

public sealed class CampaignTaskConfiguration : IEntityTypeConfiguration<CampaignTask>
{
    public void Configure(EntityTypeBuilder<CampaignTask> b)
    {
        b.ToTable("Tasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.CampaignName).HasMaxLength(250);
        b.Property(x => x.AssignedTo).HasMaxLength(150);
        b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.CampaignId);
        b.HasIndex(x => x.CreatedDate);
    }
}

public sealed class DeliverableConfiguration : IEntityTypeConfiguration<Deliverable>
{
    public void Configure(EntityTypeBuilder<Deliverable> b)
    {
        b.ToTable("Deliverables");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.CampaignName).HasMaxLength(250);
        b.Property(x => x.ClientName).HasMaxLength(200);
        b.Property(x => x.CreatorName).HasMaxLength(200);
        b.Property(x => x.Type).HasMaxLength(60);
        b.Property(x => x.Platform).HasMaxLength(60);
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.FileUrl).HasMaxLength(500);
        b.HasIndex(x => x.CampaignId);
        b.HasIndex(x => x.CreatorId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedDate);
    }
}

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> b)
    {
        b.ToTable("Notes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Content).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(40);
        b.Property(x => x.EntityName).HasMaxLength(250);
        b.Property(x => x.Author).HasMaxLength(150);
        b.Property(x => x.Visibility).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.EntityType);
        b.HasIndex(x => x.EntityId);
        b.HasIndex(x => x.CreatedDate);
    }
}
