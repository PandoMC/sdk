namespace MissionControl.Client;

/// <summary>
/// Represents a MissionControl API environment, encapsulating its base URLs and OAuth scope.
/// </summary>
public enum MissionControlEnvironment
{
    /// <summary>https://api.sandbox.missioncontrol.io</summary>
    Sandbox,

    /// <summary>https://api.missioncontrol.io</summary>
    Production,
}

internal static class MissionControlEnvironmentExtensions
{
    private static readonly Dictionary<MissionControlEnvironment, (string BaseUrl, string AssetsBaseUrl, string Scope)> Settings = new()
    {
        [MissionControlEnvironment.Sandbox] = (
            "https://api.sandbox.missioncontrol.io/partnerApi/v2",
            "https://api.sandbox.missioncontrol.io/assets/partnerApi/v1",
            "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default"),
        [MissionControlEnvironment.Production] = (
            "https://api.missioncontrol.io/partnerApi/v2",
            "https://api.missioncontrol.io/assets/partnerApi/v1",
            "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default"),
    };

    internal static string BaseUrl(this MissionControlEnvironment env) => Settings[env].BaseUrl;
    internal static string AssetsBaseUrl(this MissionControlEnvironment env) => Settings[env].AssetsBaseUrl;
    internal static string Scope(this MissionControlEnvironment env) => Settings[env].Scope;
}
