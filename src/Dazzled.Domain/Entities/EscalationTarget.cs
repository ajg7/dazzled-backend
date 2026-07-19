using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("EscalationTargets")]
public class EscalationTarget
{
    [Key]
    public int Id { get; set; }
    public int StepId { get; set; }
    public TargetType TargetType { get; set; }
    [MaxLength(64)]
    public required string TargetId { get; set; }
}
