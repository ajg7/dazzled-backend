using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("OnCallSchedules")]
public class OnCallSchedule
{
    [Key]
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
    public int TeamId { get; set; }
    public RotationType RotationType { get; set; }
}
