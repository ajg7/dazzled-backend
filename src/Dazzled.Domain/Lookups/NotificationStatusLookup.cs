using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Domain.Lookups;

public class NotificationStatusLookup
{
    public NotificationStatuses Id { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
}
