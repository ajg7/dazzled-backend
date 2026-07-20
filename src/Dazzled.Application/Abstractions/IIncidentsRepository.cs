using Ardalis.Result;
using Dazzled.Application.Incidents;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface IIncidentsRepository
{
    Task<Result<List<Incident>>> GetIncidentsAsync(IncidentFilter filter, CancellationToken ct = default);

    Task<Result<Incident>> GetIncidentByIdAsync(int incidentId, CancellationToken ct = default);

    Task<Result<List<IncidentEvent>>> GetTimelineAsync(int incidentId, CancellationToken ct = default);

    Task<Result<Incident>> AckIncidentAsync(int incidentId, Guid actorUserId, CancellationToken ct = default);

    Task<Result<Incident>> ResolveIncidentAsync(int incidentId, Guid actorUserId, CancellationToken ct = default);

    Task<Result<IncidentEvent>> AddNoteAsync(int incidentId, Guid actorUserId, string note, CancellationToken ct = default);

    Task<Result<IncidentEvent>> AddEventAsync(int incidentId, string eventType, Guid? actorUserId, string? note, CancellationToken ct = default);
}
