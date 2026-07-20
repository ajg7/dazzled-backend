namespace Dazzled.Contracts;

public enum NotificationChannel
{
    SMS,
    Voice,
    Email
}

public record AlertReceived(int AlertId, int ServiceId, string DedupKey, string Fingerprint);

public record IncidentTriggered(int IncidentId, int ServiceId, int PolicyId, int StepOrder);

public record NotificationRequested(int IncidentId, Guid UserId, NotificationChannel Channel);
