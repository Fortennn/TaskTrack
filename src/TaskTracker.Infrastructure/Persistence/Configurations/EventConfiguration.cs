using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.Description)
            .HasMaxLength(4000);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Latitude);

        builder.Property(e => e.Longitude);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Capacity)
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .IsRequired();
    }
}
