using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dazzled.Domain.Entities;

[Table("Users")]
public class User
{
    [Key]
    public Guid Id { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }
    [MaxLength(20)]
    [Phone]
    public string? PhoneE164 { get; set; }
    [MaxLength(256)]
    public required string PasswordHash { get; set; }
    public required UserRole Role { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
