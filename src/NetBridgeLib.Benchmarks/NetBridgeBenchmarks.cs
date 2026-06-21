using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NetBridgeLib.Enums;
using NetBridgeLib.Services;

BenchmarkRunner.Run<NetBridgeBenchmarks>(
    DefaultConfig.Instance
        .AddJob(Job.ShortRun.WithWarmupCount(3).WithIterationCount(5))
        .WithOptions(ConfigOptions.DisableOptimizationsValidator));

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class NetBridgeBenchmarks
{
    [BenchmarkCategory("ParseRuleAction"), Benchmark(Baseline = true)]
    public NetRuleAction ParseRuleAction_Proxy() => NetBridgeService.ParseRuleAction("PROXY");

    [BenchmarkCategory("ParseRuleAction"), Benchmark]
    public NetRuleAction ParseRuleAction_Direct() => NetBridgeService.ParseRuleAction("DIRECT");

    [BenchmarkCategory("ParseRuleAction"), Benchmark]
    public NetRuleAction ParseRuleAction_Block() => NetBridgeService.ParseRuleAction("BLOCK");

    [BenchmarkCategory("ParseRuleAction"), Benchmark]
    public NetRuleAction ParseRuleAction_LowerCase() => NetBridgeService.ParseRuleAction("proxy");

    [BenchmarkCategory("ParseRuleProtocol"), Benchmark(Baseline = true)]
    public NetRuleProtocol ParseRuleProtocol_Tcp() => NetBridgeService.ParseRuleProtocol("TCP");

    [BenchmarkCategory("ParseRuleProtocol"), Benchmark]
    public NetRuleProtocol ParseRuleProtocol_Udp() => NetBridgeService.ParseRuleProtocol("UDP");

    [BenchmarkCategory("ParseRuleProtocol"), Benchmark]
    public NetRuleProtocol ParseRuleProtocol_Both() => NetBridgeService.ParseRuleProtocol("BOTH");

    [BenchmarkCategory("ParseRuleProtocol"), Benchmark]
    public NetRuleProtocol ParseRuleProtocol_TcpUdp() => NetBridgeService.ParseRuleProtocol("TCP+UDP");

    [BenchmarkCategory("ParseRuleProtocol"), Benchmark]
    public NetRuleProtocol ParseRuleProtocol_LowerCase() => NetBridgeService.ParseRuleProtocol("tcp");

    [BenchmarkCategory("OldVersion_Ternary"), Benchmark]
    public NetRuleAction Old_ParseRuleAction_Ternary()
    {
        var action = "PROXY";
        return action.Equals("DIRECT", StringComparison.CurrentCultureIgnoreCase) ? NetRuleAction.DIRECT :
               action.Equals("BLOCK", StringComparison.CurrentCultureIgnoreCase) ? NetRuleAction.BLOCK :
               NetRuleAction.PROXY;
    }

    [BenchmarkCategory("OldVersion_Ternary"), Benchmark]
    public NetRuleProtocol Old_ParseRuleProtocol_Ternary()
    {
        var protocol = "*";
        return protocol.Equals("UDP", StringComparison.CurrentCultureIgnoreCase) ? NetRuleProtocol.UDP :
               protocol.Equals("BOTH", StringComparison.CurrentCultureIgnoreCase) || protocol.Equals("TCP+UDP", StringComparison.CurrentCultureIgnoreCase) ? NetRuleProtocol.BOTH :
               NetRuleProtocol.TCP;
    }

    [BenchmarkCategory("OldVersion_Switch"), Benchmark]
    public NetRuleAction Old_ParseRuleAction_Switch()
    {
        var action = "PROXY";
        return action?.ToUpperInvariant() switch
        {
            "DIRECT" => NetRuleAction.DIRECT,
            "BLOCK" => NetRuleAction.BLOCK,
            _ => NetRuleAction.PROXY
        };
    }

    [BenchmarkCategory("OldVersion_Switch"), Benchmark]
    public NetRuleProtocol Old_ParseRuleProtocol_Switch()
    {
        var protocol = "*";
        return protocol?.ToUpperInvariant() switch
        {
            "UDP" => NetRuleProtocol.UDP,
            "BOTH" or "TCP+UDP" => NetRuleProtocol.BOTH,
            _ => NetRuleProtocol.TCP
        };
    }

    private readonly string _processNames = "chrome.exe,firefox.exe,msedge.exe,Code.exe";

    [BenchmarkCategory("ProcessParsing"), Benchmark(Baseline = true)]
    public List<string> ParseProcessNames_Current()
    {
        return [.. _processNames
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    [BenchmarkCategory("ProcessParsing"), Benchmark]
    public List<string> ParseProcessNames_Old()
    {
        return [.. _processNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))];
    }

    private static readonly Action<string> s_staticLogHandler = _ => { };
    private static readonly Action<string, uint, string, ushort, string> s_staticConnectionHandler = (_, _, _, _, _) => { };

    [BenchmarkCategory("Callback"), Benchmark]
    public void Dispatch_StaticSingleton()
    {
        var handler = s_staticLogHandler;
        handler("test message");
    }

    [BenchmarkCategory("Callback"), Benchmark]
    public void Dispatch_DirectInvoke()
    {
        s_staticLogHandler.Invoke("test message");
    }

    [BenchmarkCategory("StringComparison"), Benchmark(Baseline = true)]
    public bool StringCompare_CurrentCulture()
    {
        return "HTTP".Equals("http", StringComparison.CurrentCultureIgnoreCase);
    }

    [BenchmarkCategory("StringComparison"), Benchmark]
    public bool StringCompare_Ordinal()
    {
        return "HTTP".Equals("http", StringComparison.OrdinalIgnoreCase);
    }

    [BenchmarkCategory("StringComparison"), Benchmark]
    public bool StringCompare_ToupperInvariant()
    {
        return "HTTP".ToUpperInvariant() == "HTTP";
    }

    private readonly System.Text.StringBuilder _reusableBuffer = new(4096);

    [BenchmarkCategory("StringBuilder"), Benchmark(Baseline = true)]
    public int StringBuilder_Reuse()
    {
        _reusableBuffer.Clear();
        _reusableBuffer.Append("result");
        return _reusableBuffer.Length;
    }

    [BenchmarkCategory("StringBuilder"), Benchmark]
    public int StringBuilder_NewEachTime()
    {
        var buffer = new System.Text.StringBuilder(4096);
        buffer.Append("result");
        return buffer.Length;
    }

    [BenchmarkCategory("ServiceLifecycle"), Benchmark]
    public void ServiceConstructor_Dispose()
    {
        try
        {
            var service = new NetBridgeService();
            service.Dispose();
        }
        catch (DllNotFoundException) { }
    }
}
