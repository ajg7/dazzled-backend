using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("Alerts")]
public class Alert
{
    [Key]
    public int Id { get; set; }
    public int ServiceId { get; set; }
    [MaxLength(200)]
    public required string Fingerprint { get; set; }
    public required string RawPayload { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}
