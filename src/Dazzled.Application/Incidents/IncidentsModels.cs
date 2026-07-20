using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Application.Incidents;

public record IncidentFilter(
    List<IncidentStatuses>? Statuses,
    List<Severity>? Severities,
    int? ServiceId,
    DateTimeOffset? TriggeredAfterUtc,
    DateTimeOffset? TriggeredBeforeUtc);

public record IncidentNoteRequest(
    [Required][MaxLength(2000)] string Note);
