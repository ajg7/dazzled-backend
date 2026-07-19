using Dazzled.Domain.Entities;
using Dazzled.Domain.Enums;
using Dazzled.Domain.Lookups;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class DazzledDbContext(DbContextOptions<DazzledDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<EscalationPolicy> EscalationPolicies => Set<EscalationPolicy>();
    public DbSet<EscalationStep> EscalationSteps => Set<EscalationStep>();
    public DbSet<EscalationTarget> EscalationTargets => Set<EscalationTarget>();
    public DbSet<OnCallSchedule> OnCallSchedules => Set<OnCallSchedule>();
    public DbSet<ScheduleMember> ScheduleMembers => Set<ScheduleMember>();
    public DbSet<ScheduleOverride> ScheduleOverrides => Set<ScheduleOverride>();
    public DbSet<IncidentStatusLookup> IncidentStatuses => Set<IncidentStatusLookup>();
    public DbSet<NotificationStatusLookup> NotificationStatuses => Set<NotificationStatusLookup>();
    public DbSet<ChannelLookup> Channels => Set<ChannelLookup>();
    public DbSet<SeverityLookup> Severities => Set<SeverityLookup>();

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

        modelBuilder.Entity<SeverityLookup>().HasData(
            Enum.GetValues<Severity>().Select(severity => new SeverityLookup
            {
                Id = severity,
                Name = severity.ToString()
            }));

        // FK constraints tying the enum-backed columns to their reference tables,
        // so the database itself rejects a value outside the enum. Restrict, since
        // deleting a lookup row should never cascade into deleting real records.
        modelBuilder.Entity<Incident>()
            .HasOne<IncidentStatusLookup>()
            .WithMany()
            .HasForeignKey(incident => incident.Status)
            .HasPrincipalKey(status => status.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne<SeverityLookup>()
            .WithMany()
            .HasForeignKey(incident => incident.Severity)
            .HasPrincipalKey(severity => severity.Id)
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

        // Backs the email-uniqueness checks in UserRepository, which are otherwise
        // racy: two concurrent creates can both pass the pre-insert lookup.
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Team>().HasIndex(team => team.Name).IsUnique();

        modelBuilder.Entity<TeamMember>().HasKey(member => new { member.TeamId, member.UserId });

        // Config-graph relationships. Restrict on delete to avoid SQL Server
        // multiple-cascade-path errors; cleanup is handled in application logic.
        modelBuilder.Entity<TeamMember>()
            .HasOne<Team>().WithMany().HasForeignKey(member => member.TeamId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TeamMember>()
            .HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Service>()
            .HasOne<Team>().WithMany().HasForeignKey(service => service.TeamId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Service>()
            .HasOne<EscalationPolicy>().WithMany().HasForeignKey(service => service.EscalationPolicyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Service>().HasIndex(service => service.IntegrationKey).IsUnique();

        modelBuilder.Entity<EscalationPolicy>()
            .HasOne<Team>().WithMany().HasForeignKey(policy => policy.TeamId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EscalationStep>()
            .HasOne<EscalationPolicy>().WithMany().HasForeignKey(step => step.PolicyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EscalationTarget>()
            .HasOne<EscalationStep>().WithMany().HasForeignKey(target => target.StepId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OnCallSchedule>()
            .HasOne<Team>().WithMany().HasForeignKey(schedule => schedule.TeamId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleMember>()
            .HasOne<OnCallSchedule>().WithMany().HasForeignKey(member => member.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleMember>()
            .HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleOverride>()
            .HasOne<OnCallSchedule>().WithMany().HasForeignKey(ovr => ovr.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleOverride>()
            .HasOne<User>().WithMany().HasForeignKey(ovr => ovr.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Alert>()
            .HasOne<Service>().WithMany().HasForeignKey(alert => alert.ServiceId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne<Service>().WithMany().HasForeignKey(incident => incident.ServiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Incident>().HasIndex(incident => new { incident.ServiceId, incident.DedupKey });

        modelBuilder.Entity<IncidentEvent>()
            .HasOne<Incident>().WithMany().HasForeignKey(evt => evt.IncidentId).OnDelete(DeleteBehavior.Restrict);
    }
}
