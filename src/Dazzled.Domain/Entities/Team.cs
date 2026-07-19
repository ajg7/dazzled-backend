using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("Teams")]
public class Team
{
    [Key]
    public int Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
}
