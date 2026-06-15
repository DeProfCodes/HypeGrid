using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Alerts;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class AlertSubscriberConfiguration : IEntityTypeConfiguration<AlertSubscriber>
{
    public void Configure(EntityTypeBuilder<AlertSubscriber> b)
    {
        b.ToTable("AlertSubscribers");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(200);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.Province).HasMaxLength(120);
        b.Property(x => x.FrequencyPreference).HasMaxLength(50);
        b.Property(x => x.SourcePage).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.ConsentTextVersion).HasMaxLength(100);
        // ConsentSnapshot is the exact copy shown — leave unbounded (nvarchar(max)).
        b.Property(x => x.ConsentIpHash).HasMaxLength(128);
        b.Property(x => x.ConsentUserAgent).HasMaxLength(512);

        // EF Core 8 maps List<string> as a JSON primitive collection column.
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.PhoneNumber);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.ChannelJoinClicked);
    }
}
