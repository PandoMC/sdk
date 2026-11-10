using AssetsClient = MissionControl.Client.Generated.Assets.AssetsClient;

namespace MissionControl.Client.Generated;

public partial class Client
{
    private AssetsClient? _assets;

    /// <summary>
    /// The Assets API: asset packs (images, logos, etc.) publishers provide for their products.
    /// It is served by a separate service, so it has its own base URL but shares authentication and headers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the client was not created with <see cref="ClientBuilder"/>.</exception>
    public AssetsClient Assets
    {
        get => _assets ?? throw new InvalidOperationException(
            $"The Assets API is only available on clients created with {nameof(ClientBuilder)}.");
        internal set => _assets = value;
    }
}
