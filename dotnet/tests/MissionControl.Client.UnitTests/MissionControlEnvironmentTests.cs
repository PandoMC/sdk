namespace MissionControl.Client.UnitTests;

public class MissionControlEnvironmentTests
{
    [Theory]
    [InlineData(MissionControlEnvironment.Development, "https://api.dev.missioncontrol.io")]
    [InlineData(MissionControlEnvironment.Test,        "https://api.test.missioncontrol.io")]
    [InlineData(MissionControlEnvironment.Stage,       "https://api.stage.missioncontrol.io")]
    [InlineData(MissionControlEnvironment.Sandbox,     "https://api.sandbox.missioncontrol.io")]
    [InlineData(MissionControlEnvironment.Production,  "https://api.missioncontrol.io")]
    public void BaseUrl_ReturnsExpectedUrl(MissionControlEnvironment env, string expected)
    {
        Assert.Equal(expected, env.BaseUrl());
    }

    [Theory]
    [InlineData(MissionControlEnvironment.Development, "api://7a3c5c50-f9b2-4ab3-add4-3a0189c89360/.default")]
    [InlineData(MissionControlEnvironment.Test,        "api://0acf850f-2038-43bb-adea-69a1aa69b61c/.default")]
    [InlineData(MissionControlEnvironment.Stage,       "api://7ff91c8f-8b67-4a8f-b444-9669f39289eb/.default")]
    [InlineData(MissionControlEnvironment.Sandbox,     "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default")]
    [InlineData(MissionControlEnvironment.Production,  "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default")]
    public void Scope_ReturnsExpectedScope(MissionControlEnvironment env, string expected)
    {
        Assert.Equal(expected, env.Scope());
    }
}
