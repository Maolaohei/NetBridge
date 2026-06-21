using NetBridgeLib.Enums;
using NetBridgeLib.Services;

namespace NetBridgeLib.Tests;

public class ParseRuleActionTests
{
    [Theory]
    [InlineData("PROXY", NetRuleAction.PROXY)]
    [InlineData("DIRECT", NetRuleAction.DIRECT)]
    [InlineData("BLOCK", NetRuleAction.BLOCK)]
    public void KnownValues_ParseCorrectly(string input, NetRuleAction expected)
    {
        Assert.Equal(expected, NetBridgeService.ParseRuleAction(input));
    }

    [Theory]
    [InlineData("proxy")]
    [InlineData("Proxy")]
    [InlineData("PROXY")]
    [InlineData("pRoXy")]
    public void CaseInsensitive_MatchesCorrectly(string input)
    {
        Assert.Equal(NetRuleAction.PROXY, NetBridgeService.ParseRuleAction(input));
    }

    [Theory]
    [InlineData("BLCK")]
    [InlineData("BLOK")]
    [InlineData("REJECT")]
    [InlineData("ALLOW")]
    public void UnknownValues_ThrowArgumentException(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleAction(input));
        Assert.Contains(input, ex.Message);
    }

    [Fact]
    public void Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleAction(null));
    }

    [Fact]
    public void EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleAction(""));
    }

    [Fact]
    public void Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleAction("   "));
    }

    [Fact]
    public void BreakingChange_SilentDefaultNoLongerApplies()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleAction("INVALID"));
    }
}

public class ParseRuleProtocolTests
{
    [Theory]
    [InlineData("TCP", NetRuleProtocol.TCP)]
    [InlineData("UDP", NetRuleProtocol.UDP)]
    [InlineData("BOTH", NetRuleProtocol.BOTH)]
    [InlineData("TCP+UDP", NetRuleProtocol.BOTH)]
    public void KnownValues_ParseCorrectly(string input, NetRuleProtocol expected)
    {
        Assert.Equal(expected, NetBridgeService.ParseRuleProtocol(input));
    }

    [Theory]
    [InlineData("tcp")]
    [InlineData("Tcp")]
    [InlineData("TCP")]
    public void CaseInsensitive_MatchesCorrectly(string input)
    {
        Assert.Equal(NetRuleProtocol.TCP, NetBridgeService.ParseRuleProtocol(input));
    }

    [Theory]
    [InlineData("ICMP")]
    [InlineData("HTTP")]
    [InlineData("ALL")]
    [InlineData("*")]
    public void UnknownValues_ThrowArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleProtocol(input));
    }

    [Fact]
    public void Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleProtocol(null));
    }

    [Fact]
    public void EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleProtocol(""));
    }

    [Fact]
    public void BreakingChange_SilentDefaultNoLongerApplies()
    {
        Assert.Throws<ArgumentException>(() => NetBridgeService.ParseRuleProtocol("INVALID"));
    }
}
