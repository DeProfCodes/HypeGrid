using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Finance;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> b)
    {
        b.ToTable("Quotes");
        b.HasKey(x => x.Id);
        b.Property(x => x.QuoteNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(200);
        b.Property(x => x.CampaignType).HasMaxLength(100);
        b.Property(x => x.PackageName).HasMaxLength(100);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        // LineItems is a complex collection → store as a JSON column.
        b.OwnsMany(x => x.LineItems, nav =>
        {
            nav.ToJson();
            nav.Property(li => li.Amount).HasColumnType("decimal(18,2)");
        });
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ClientId);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasKey(x => x.Id);
        b.Property(x => x.InvoiceNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(200);
        b.Property(x => x.CampaignName).HasMaxLength(250);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Outstanding).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.OwnsMany(x => x.LineItems, nav =>
        {
            nav.ToJson();
            nav.Property(li => li.Amount).HasColumnType("decimal(18,2)");
        });
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ClientId);
    }
}

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> b)
    {
        b.ToTable("Payouts");
        b.HasKey(x => x.Id);
        b.Property(x => x.CreatorName).HasMaxLength(200);
        b.Property(x => x.CampaignName).HasMaxLength(250);
        b.Property(x => x.Deliverable).HasMaxLength(300);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatorId);
    }
}
