using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("Services")]
public class Service
{
    [Key]
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
    public Guid IntegrationKey { get; set; }
    public int? EscalationPolicyId { get; set; }
    public int TeamId { get; set; }
}
