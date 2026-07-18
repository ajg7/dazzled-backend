using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain;

[Table("Notifications")]
public class Notification
{
    [Key]
    public int NotificationId { get; set; }
    public int IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required Channels Channel { get; set; }
    public required NotificationStatuses Status { get; set; }
    public DateTimeOffset DateSent { get; set; }
}
