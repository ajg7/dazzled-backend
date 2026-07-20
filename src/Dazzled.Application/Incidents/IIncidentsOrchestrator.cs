using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Incidents;

public interface IIncidentsOrchestrator
{
    Task<Result<List<Incident>>> GetIncidents(IncidentFilter filter, CancellationToken ct = default);

    Task<Result<Incident>> GetIncidentById(int incidentId, CancellationToken ct = default);

    Task<Result<List<IncidentEvent>>> GetTimeline(int incidentId, CancellationToken ct = default);

    Task<Result<Incident>> AckIncident(int incidentId, Guid actorUserId, CancellationToken ct = default);

    Task<Result<Incident>> ResolveIncident(int incidentId, Guid actorUserId, CancellationToken ct = default);

    Task<Result<IncidentEvent>> AddNote(int incidentId, Guid actorUserId, IncidentNoteRequest request, CancellationToken ct = default);
}
