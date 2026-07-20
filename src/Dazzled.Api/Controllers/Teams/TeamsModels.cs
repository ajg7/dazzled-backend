using System.ComponentModel.DataAnnotations;

namespace Dazzled.Api.Controllers.Teams;

public record TeamResponse(int Id, string Name);

public record TeamCreationRequest(
    [Required][MaxLength(200)] string Name);

public record TeamMemberRequest(
    [Required] Guid UserId);

public record TeamMemberResponse(int TeamId, Guid UserId);

public record TeamMembersResponse(List<TeamMemberResponse> Members);
