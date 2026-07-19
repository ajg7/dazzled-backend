using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("EscalationSteps")]
public class EscalationStep
{
    [Key]
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int StepOrder { get; set; }
    public int TimeoutMinutes { get; set; }
}
