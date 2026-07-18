using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Domain.Lookups;

public class IncidentStatusLookup
{
    public IncidentStatuses Id { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
}
