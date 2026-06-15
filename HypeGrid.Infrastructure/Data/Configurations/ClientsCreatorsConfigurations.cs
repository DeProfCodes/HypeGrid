using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HypeGrid.Domain.Clients;
using HypeGrid.Domain.Creators;

namespace HypeGrid.Infrastructure.Data.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("Clients");
        b.HasKey(x => x.Id);
        b.Property(x => x.BrandName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactPerson).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.ClientType).HasMaxLength(100);
        b.Property(x => x.Industry).HasMaxLength(150);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.Website).HasMaxLength(300);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.TotalSpend).HasColumnType("decimal(18,2)");
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
    }
}

public sealed class CreatorConfiguration : IEntityTypeConfiguration<Creator>
{
    public void Configure(EntityTypeBuilder<Creator> b)
    {
        b.ToTable("Creators");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Handle).HasMaxLength(150);
        b.Property(x => x.Platform).HasMaxLength(50);
        b.Property(x => x.Niche).HasMaxLength(50);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.Province).HasMaxLength(120);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.TotalEarned).HasColumnType("decimal(18,2)");
        b.Property(x => x.TotalPaid).HasColumnType("decimal(18,2)");
        b.HasIndex(x => x.CreatedDate);
        b.HasIndex(x => x.Status);
    }
}
