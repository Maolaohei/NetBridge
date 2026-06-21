using System.Text.Json;
using NetBridgeLib.Enums;

namespace NetBridgeLib.Tests;

public class EnumSerializationTests
{
    [Theory]
    [InlineData(NetRuleAction.PROXY, 0)]
    [InlineData(NetRuleAction.DIRECT, 1)]
    [InlineData(NetRuleAction.BLOCK, 2)]
    public void NetRuleAction_HasExpectedNumericValues(NetRuleAction action, int expected)
    {
        Assert.Equal(expected, (int)action);
    }

    [Theory]
    [InlineData(NetRuleProtocol.TCP, 0)]
    [InlineData(NetRuleProtocol.UDP, 1)]
    [InlineData(NetRuleProtocol.BOTH, 2)]
    public void NetRuleProtocol_HasExpectedNumericValues(NetRuleProtocol protocol, int expected)
    {
        Assert.Equal(expected, (int)protocol);
    }

    [Theory]
    [InlineData(NetProxyType.HTTP, 0)]
    [InlineData(NetProxyType.SOCKS5, 1)]
    public void NetProxyType_HasExpectedNumericValues(NetProxyType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }

    [Fact]
    public void EnumValues_MatchNativeDllExpectedValues()
    {
        Assert.Equal(0, (int)NetProxyType.HTTP);
        Assert.Equal(1, (int)NetProxyType.SOCKS5);
        Assert.Equal(0, (int)NetRuleAction.PROXY);
        Assert.Equal(1, (int)NetRuleAction.DIRECT);
        Assert.Equal(2, (int)NetRuleAction.BLOCK);
        Assert.Equal(0, (int)NetRuleProtocol.TCP);
        Assert.Equal(1, (int)NetRuleProtocol.UDP);
        Assert.Equal(2, (int)NetRuleProtocol.BOTH);
    }
}
