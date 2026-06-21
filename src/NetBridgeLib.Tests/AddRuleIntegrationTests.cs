using NetBridgeLib.Enums;
using NetBridgeLib.Services;

namespace NetBridgeLib.Tests;

/// <summary>
/// 验证 AddRule 的参数解析链路与 ViewModel 调用方一致。
/// 回归测试：ApplyRulesFromInput 传入的参数必须能被 ParseRuleAction/ParseRuleProtocol 正确解析。
/// </summary>
public class AddRuleIntegrationTests
{
    private static readonly string[] CommonProcessNames =
    [
        "Chrome.exe",
        "Firefox.exe",
        "msedge.exe",
        "Code.exe",
        "devenv.exe",
    ];

    [Fact]
    public void ApplyRulesFromInput_Parameters_DoNotThrow()
    {
        foreach (var processName in CommonProcessNames)
        {
            var ex = Record.Exception(() => SimulateAddRuleParsing(processName, "*", "*", "BOTH", "PROXY"));
            Assert.Null(ex);
        }
    }

    [Theory]
    [InlineData("TCP")]
    [InlineData("UDP")]
    [InlineData("BOTH")]
    [InlineData("TCP+UDP")]
    public void ValidProtocolValues_DoNotThrow(string protocol)
    {
        var ex = Record.Exception(() => SimulateAddRuleParsing("test.exe", "*", "*", protocol, "PROXY"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("DIRECT")]
    [InlineData("BLOCK")]
    [InlineData("PROXY")]
    public void ValidActionValues_DoNotThrow(string action)
    {
        var ex = Record.Exception(() => SimulateAddRuleParsing("test.exe", "*", "*", "BOTH", action));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("ALL")]
    [InlineData("ICMP")]
    [InlineData("HTTP")]
    public void InvalidProtocolValues_ThrowArgumentException(string protocol)
    {
        Assert.Throws<ArgumentException>(() => SimulateAddRuleParsing("test.exe", "*", "*", protocol, "PROXY"));
    }

    [Theory]
    [InlineData("ALLOW")]
    [InlineData("REJECT")]
    [InlineData("REDIRECT")]
    public void InvalidActionValues_ThrowArgumentException(string action)
    {
        Assert.Throws<ArgumentException>(() => SimulateAddRuleParsing("test.exe", "*", "*", "BOTH", action));
    }

    [Fact]
    public void AllViewModelParameterCombinations_Succeed()
    {
        var actions = new[] { "PROXY", "DIRECT", "BLOCK" };
        var protocols = new[] { "TCP", "UDP", "BOTH", "TCP+UDP" };

        foreach (var action in actions)
        {
            foreach (var protocol in protocols)
            {
                var ex = Record.Exception(() => SimulateAddRuleParsing("Chrome.exe", "*", "*", protocol, action));
                Assert.Null(ex);
            }
        }
    }

    private static void SimulateAddRuleParsing(
        string processName, string targetHosts, string targetPorts,
        string protocol, string action, uint proxyConfigId = 0)
    {
        var ruleAction = NetBridgeService.ParseRuleAction(action);
        var ruleProtocol = NetBridgeService.ParseRuleProtocol(protocol);
    }
}
