using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Incidents;

public class IncidentsOrchestrator(IIncidentsRepository incidentsRepository) : IIncidentsOrchestrator
{
    public Task<Result<List<Incident>>> GetIncidents(IncidentFilter filter, CancellationToken ct = default) =>
        incidentsRepository.GetIncidentsAsync(filter, ct);

    public Task<Result<Incident>> GetIncidentById(int incidentId, CancellationToken ct = default) =>
        incidentsRepository.GetIncidentByIdAsync(incidentId, ct);

    public Task<Result<List<IncidentEvent>>> GetTimeline(int incidentId, CancellationToken ct = default) =>
        incidentsRepository.GetTimelineAsync(incidentId, ct);

    /// <remarks>
    /// Phase 4.3 adds two more steps here once the escalation engine exists:
    /// cancel the Hangfire job on <see cref="Incident.HangfireJobId"/> so the chain
    /// stops, and broadcast incidentUpdated over SignalR. Until then an acked incident
    /// still records correctly but nothing halts an in-flight escalation.
    /// </remarks>
    public Task<Result<Incident>> AckIncident(int incidentId, Guid actorUserId, CancellationToken ct = default) =>
        incidentsRepository.AckIncidentAsync(incidentId, actorUserId, ct);

    /// <inheritdoc cref="AckIncident"/>
    public Task<Result<Incident>> ResolveIncident(int incidentId, Guid actorUserId, CancellationToken ct = default) =>
        incidentsRepository.ResolveIncidentAsync(incidentId, actorUserId, ct);

    public Task<Result<IncidentEvent>> AddNote(int incidentId, Guid actorUserId, IncidentNoteRequest request, CancellationToken ct = default) =>
        incidentsRepository.AddNoteAsync(incidentId, actorUserId, request.Note, ct);
}
