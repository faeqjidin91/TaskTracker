using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Infrastructure.Notifications;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLog");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Message).IsRequired().HasMaxLength(NotificationLog.MessageMaxLength);
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        builder.HasIndex(n => n.TaskId);
    }
}
