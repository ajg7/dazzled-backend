using Dazzled.Domain.Entities;

namespace Dazzled.Api.Controllers.Teams;

public record TeamResponse(int Id, string Name);

public record TeamCreationRequest(string Name);

public record TeamMembersResponse(List<TeamMember> Members);