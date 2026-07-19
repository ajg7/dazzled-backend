using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("IncidentEvents")]
public class IncidentEvent
{
    [Key]
    public int Id { get; set; }
    public int IncidentId { get; set; }
    [MaxLength(100)]
    public required string EventType { get; set; }
    public Guid? ActorUserId { get; set; }
    [MaxLength(2000)]
    public string? Note { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
