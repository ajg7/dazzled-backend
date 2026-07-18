using Dazzled.Domain;
using Dazzled.Domain.Enums;
using Dazzled.Domain.Lookups;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class DazzledDbContext(DbContextOptions<DazzledDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<IncidentStatusLookup> IncidentStatuses => Set<IncidentStatusLookup>();
    public DbSet<NotificationStatusLookup> NotificationStatuses => Set<NotificationStatusLookup>();
    public DbSet<ChannelLookup> Channels => Set<ChannelLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<IncidentStatusLookup>().HasData(
            Enum.GetValues<IncidentStatuses>().Select(status => new IncidentStatusLookup
            {
                Id = status,
                Name = status.ToString()
            }));

        modelBuilder.Entity<NotificationStatusLookup>().HasData(
            Enum.GetValues<NotificationStatuses>().Select(status => new NotificationStatusLookup
            {
                Id = status,
                Name = status.ToString()
            }));

        modelBuilder.Entity<ChannelLookup>().HasData(
            Enum.GetValues<Channels>().Select(channel => new ChannelLookup
            {
                Id = channel,
                Name = channel.ToString()
            }));

        // FK constraints tying the enum-backed columns to their reference tables,
        // so the database itself rejects a Status/Channel value outside the enum.
        // Restrict (not Cascade) since deleting a lookup row should never cascade
        // into deleting real incidents/notifications.
        modelBuilder.Entity<Incident>()
            .HasOne<IncidentStatusLookup>()
            .WithMany()
            .HasForeignKey(incident => incident.Status)
            .HasPrincipalKey(status => status.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne<NotificationStatusLookup>()
            .WithMany()
            .HasForeignKey(notification => notification.Status)
            .HasPrincipalKey(status => status.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne<ChannelLookup>()
            .WithMany()
            .HasForeignKey(notification => notification.Channel)
            .HasPrincipalKey(channel => channel.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
