using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("TeamMembers")]
public class TeamMember
{
    public int TeamId { get; set; }
    public Guid UserId { get; set; }
}
