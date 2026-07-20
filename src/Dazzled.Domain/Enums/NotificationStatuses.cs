namespace Dazzled.Domain.Enums;
public enum NotificationStatuses
{
    None,
    Sent,
    Delivered,
    Failed,
    // Appended, never inserted: these values are persisted as ints, so reordering
    // would silently remap every existing Notification row.
    Acked
}