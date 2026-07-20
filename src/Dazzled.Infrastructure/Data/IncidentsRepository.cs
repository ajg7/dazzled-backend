using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Application.Incidents;
using Dazzled.Domain.Entities;
using Dazzled.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class IncidentsRepository(DazzledDbContext db) : IIncidentsRepository
{
    public async Task<Result<List<Incident>>> GetIncidentsAsync(IncidentFilter filter, CancellationToken ct = default)
    {
        try
        {
            var query = db.Incidents.AsQueryable();

            if (filter.Statuses is { Count: > 0 })
                query = query.Where(incident => filter.Statuses.Contains(incident.Status));

            if (filter.Severities is { Count: > 0 })
                query = query.Where(incident => filter.Severities.Contains(incident.Severity));

            if (filter.ServiceId.HasValue)
                query = query.Where(incident => incident.ServiceId == filter.ServiceId.Value);

            if (filter.TriggeredAfterUtc.HasValue)
                query = query.Where(incident => incident.DateTriggered >= filter.TriggeredAfterUtc.Value);

            if (filter.TriggeredBeforeUtc.HasValue)
                query = query.Where(incident => incident.DateTriggered <= filter.TriggeredBeforeUtc.Value);

            var incidents = await query
                .OrderByDescending(incident => incident.DateTriggered)
                .ToListAsync(ct);

            return Result.Success(incidents);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Incident>> GetIncidentByIdAsync(int incidentId, CancellationToken ct = default)
    {
        try
        {
            var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, ct);

            return incident is null
                ? Result.NotFound($"No incident with id {incidentId} exists.")
                : Result.Success(incident);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<IncidentEvent>>> GetTimelineAsync(int incidentId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Incidents.AnyAsync(incident => incident.Id == incidentId, ct))
                return Result.NotFound($"No incident with id {incidentId} exists.");

            var events = await db.IncidentEvents
                .Where(incidentEvent => incidentEvent.IncidentId == incidentId)
                .OrderBy(incidentEvent => incidentEvent.OccurredAtUtc)
                .ThenBy(incidentEvent => incidentEvent.Id)
                .ToListAsync(ct);

            return Result.Success(events);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Incident>> AckIncidentAsync(int incidentId, Guid actorUserId, CancellationToken ct = default)
    {
        try
        {
            var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, ct);

            if (incident is null)
                return Result.NotFound($"No incident with id {incidentId} exists.");

            var actor = await ValidateActorAsync(actorUserId, ct);
            if (!actor.IsSuccess)
                return actor;

            if (incident.Status == IncidentStatuses.Resolved)
                return Result.Conflict($"Incident {incidentId} is already resolved.");

            // Acking twice is what happens when two responders both get paged and both
            // tap the link. The second one is a no-op, not an error.
            if (incident.Status == IncidentStatuses.Acknowledged)
                return Result.Success(incident);

            var now = DateTimeOffset.UtcNow;

            incident.Status = IncidentStatuses.Acknowledged;
            incident.AckedAt = now;
            incident.AckedByUserId = actorUserId;

            db.IncidentEvents.Add(NewEvent(incidentId, "acked", actorUserId, note: null, now));

            var notifications = await db.Notifications
                .Where(notification =>
                    notification.IncidentId == incidentId &&
                    notification.UserId == actorUserId &&
                    notification.AckedAtUtc == null)
                .ToListAsync(ct);

            foreach (var notification in notifications)
            {
                notification.Status = NotificationStatuses.Acked;
                notification.AckedAtUtc = now;
            }

            await db.SaveChangesAsync(ct);

            return Result.Success(incident);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Incident>> ResolveIncidentAsync(int incidentId, Guid actorUserId, CancellationToken ct = default)
    {
        try
        {
            var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, ct);

            if (incident is null)
                return Result.NotFound($"No incident with id {incidentId} exists.");

            var actor = await ValidateActorAsync(actorUserId, ct);
            if (!actor.IsSuccess)
                return actor;

            if (incident.Status == IncidentStatuses.Resolved)
                return Result.Success(incident);

            var now = DateTimeOffset.UtcNow;

            incident.Status = IncidentStatuses.Resolved;
            incident.ResolvedAt = now;

            db.IncidentEvents.Add(NewEvent(incidentId, "resolved", actorUserId, note: null, now));

            await db.SaveChangesAsync(ct);

            return Result.Success(incident);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<IncidentEvent>> AddNoteAsync(int incidentId, Guid actorUserId, string note, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Incidents.AnyAsync(incident => incident.Id == incidentId, ct))
                return Result.NotFound($"No incident with id {incidentId} exists.");

            var actor = await ValidateActorAsync(actorUserId, ct);
            if (!actor.IsSuccess)
                return actor;

            var incidentEvent = NewEvent(incidentId, "note", actorUserId, note, DateTimeOffset.UtcNow);

            db.IncidentEvents.Add(incidentEvent);
            await db.SaveChangesAsync(ct);

            return Result.Created(incidentEvent);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<IncidentEvent>> AddEventAsync(int incidentId, string eventType, Guid? actorUserId, string? note, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Incidents.AnyAsync(incident => incident.Id == incidentId, ct))
                return Result.NotFound($"No incident with id {incidentId} exists.");

            if (actorUserId is { } actorId)
            {
                var actor = await ValidateActorAsync(actorId, ct);
                if (!actor.IsSuccess)
                    return actor;
            }

            var incidentEvent = NewEvent(incidentId, eventType, actorUserId, note, DateTimeOffset.UtcNow);

            db.IncidentEvents.Add(incidentEvent);
            await db.SaveChangesAsync(ct);

            return Result.Created(incidentEvent);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private async Task<Result> ValidateActorAsync(Guid actorUserId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(user => user.Id == actorUserId, ct))
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(IncidentEvent.ActorUserId),
                ErrorMessage = $"No user with id {actorUserId} exists."
            });

        return Result.Success();
    }

    private static IncidentEvent NewEvent(int incidentId, string eventType, Guid? actorUserId, string? note, DateTimeOffset occurredAtUtc) =>
        new()
        {
            IncidentId = incidentId,
            EventType = eventType,
            ActorUserId = actorUserId,
            Note = note,
            OccurredAtUtc = occurredAtUtc
        };
}
