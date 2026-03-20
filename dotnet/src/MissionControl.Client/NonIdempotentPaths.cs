namespace MissionControl.Client;

/// <summary>
/// Path prefixes for non-idempotent order-mutation endpoints.
/// Used both to suppress retries and to trigger auto-cancel on failure.
/// </summary>
internal static class NonIdempotentPaths
{
    internal static readonly string[] All =
    [
        "/Order/createOrder",
        "/Order/claimReservation",
        "/Order/reportOrder",
    ];
}
