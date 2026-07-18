using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain;

[Table("Incidents")]
public class Incident
{
    [Key]
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Title { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    public required IncidentStatuses Status { get; set; }
    public DateTimeOffset DateTriggered { get; set; }
    public DateTimeOffset? AckedAt { get; set; }
    public Guid? AckedByUserId { get; set; }
    public User? AckedByUser { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
