using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("ScheduleOverrides")]
public class ScheduleOverride
{
    [Key]
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
}
