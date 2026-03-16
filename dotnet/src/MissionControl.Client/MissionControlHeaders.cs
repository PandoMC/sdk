namespace MissionControl.Client;

/// <summary>
/// Header names used by the MissionControl Partner API.
/// </summary>
public static class MissionControlHeaders
{
    /// <summary>
    /// Identifies the partner on whose behalf the request is made.
    /// This value is provided via the portal on the client credentials page.
    /// </summary>
    public const string PartnerId = "X-Partner-Id";
}
