using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("ScheduleMembers")]
public class ScheduleMember
{
    [Key]
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public Guid UserId { get; set; }
    public int RotationOrder { get; set; }
}
