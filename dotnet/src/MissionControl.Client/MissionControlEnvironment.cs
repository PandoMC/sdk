namespace MissionControl.Client;

/// <summary>
/// Represents a MissionControl API environment, encapsulating its base URL and OAuth scope.
/// </summary>
public enum MissionControlEnvironment
{
    /// <summary>https://api.dev.missioncontrol.io</summary>
    Development,

    /// <summary>https://api.test.missioncontrol.io</summary>
    Test,

    /// <summary>https://api.stage.missioncontrol.io</summary>
    Stage,

    /// <summary>https://api.sandbox.missioncontrol.io</summary>
    Sandbox,

    /// <summary>https://api.missioncontrol.io</summary>
    Production,
}

internal static class MissionControlEnvironmentExtensions
{
    private static readonly Dictionary<MissionControlEnvironment, (string BaseUrl, string Scope)> Settings = new()
    {
        [MissionControlEnvironment.Development] = ("https://api.dev.missioncontrol.io", "api://7a3c5c50-f9b2-4ab3-add4-3a0189c89360/.default"),
        [MissionControlEnvironment.Test] = ("https://api.test.missioncontrol.io", "api://0acf850f-2038-43bb-adea-69a1aa69b61c/.default"),
        [MissionControlEnvironment.Stage] = ("https://api.stage.missioncontrol.io", "api://7ff91c8f-8b67-4a8f-b444-9669f39289eb/.default"),
        [MissionControlEnvironment.Sandbox] = ("https://api.sandbox.missioncontrol.io", "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default"),
        [MissionControlEnvironment.Production] = ("https://api.missioncontrol.io", "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default"),
    };

    internal static string BaseUrl(this MissionControlEnvironment env) => Settings[env].BaseUrl;
    internal static string Scope(this MissionControlEnvironment env) => Settings[env].Scope;
}
