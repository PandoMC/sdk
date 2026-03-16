namespace MissionControl.Client;

/// <summary>
/// A <see cref="DelegatingHandler"/> that injects a default <c>X-Partner-Id</c> header into
/// every outgoing request. The header is only added when no value has already been set on the
/// request, so a per-request override always takes precedence.
/// </summary>
internal sealed class PartnerIdHandler : DelegatingHandler
{
    private readonly string _defaultPartnerId;

    /// <summary>
    /// Initialises a new instance of <see cref="PartnerIdHandler"/>.
    /// </summary>
    /// <param name="defaultPartnerId">
    /// The partner ID to add to requests that do not already carry the header.
    /// </param>
    public PartnerIdHandler(string defaultPartnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultPartnerId);
        _defaultPartnerId = defaultPartnerId;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(MissionControlHeaders.PartnerId))
        {
            request.Headers.TryAddWithoutValidation(MissionControlHeaders.PartnerId, _defaultPartnerId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
