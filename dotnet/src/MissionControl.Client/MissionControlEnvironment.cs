namespace MissionControl.Client;

/// <summary>
/// Represents a MissionControl API environment, encapsulating its base URL and OAuth scope.
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
    private static readonly Dictionary<MissionControlEnvironment, (string BaseUrl, string Scope)> Settings = new()
    {
        [MissionControlEnvironment.Sandbox] = ("http://api.sandbox.missioncontrol.io/partnerApi/v2", "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default"),
        [MissionControlEnvironment.Production] = ("http://api.missioncontrol.io/partnerApi/v2", "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default"),
    };

    internal static string BaseUrl(this MissionControlEnvironment env) => Settings[env].BaseUrl;
    internal static string Scope(this MissionControlEnvironment env) => Settings[env].Scope;
}
