namespace Dazzled.Contracts;

public enum NotificationChannel
{
    SMS,
    Voice,
    Email
}

public record AlertReceived(Guid AlertId, Guid ServiceId, string DedupKey, string Fingerprint);

public record IncidentTriggered(Guid IncidentId, Guid ServiceId, Guid PolicyId, int StepOrder);

public record NotificationRequested(Guid IncidentId, Guid UserId, NotificationChannel Channel);
