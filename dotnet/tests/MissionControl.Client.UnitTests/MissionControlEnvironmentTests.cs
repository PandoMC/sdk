namespace MissionControl.Client.UnitTests;

public class MissionControlEnvironmentTests
{
    [Theory]
    [InlineData(MissionControlEnvironment.Sandbox, "https://api.sandbox.missioncontrol.io/partnerApi/v2")]
    [InlineData(MissionControlEnvironment.Production, "https://api.missioncontrol.io/partnerApi/v2")]
    public void BaseUrl_ReturnsExpectedUrl(MissionControlEnvironment env, string expected)
    {
        Assert.Equal(expected, env.BaseUrl());
    }

    [Theory]
    [InlineData(MissionControlEnvironment.Sandbox, "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default")]
    [InlineData(MissionControlEnvironment.Production, "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default")]
    public void Scope_ReturnsExpectedScope(MissionControlEnvironment env, string expected)
    {
        Assert.Equal(expected, env.Scope());
    }
}
