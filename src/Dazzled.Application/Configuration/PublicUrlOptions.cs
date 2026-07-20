namespace Dazzled.Application.Configuration;

/// <summary>
/// The externally reachable origin of this API. Alert sources and Twilio callbacks
/// have to reach it from outside the container network, so it cannot be inferred
/// from the listening address — in dev it is the ngrok tunnel, in production the
/// public host. Bound from the root configuration key <c>PublicBaseUrl</c>.
/// </summary>
public class PublicUrlOptions
{
    public string? PublicBaseUrl { get; set; }
}
