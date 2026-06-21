using NetBridgeLib.Enums;

namespace NetBridgeLib.Services;

public class NetBridgeService : IDisposable
{
    private static NetBridgeNative.LogCallback? s_staticLogHandler;
    private static NetBridgeNative.ConnectionCallback? s_staticConnectionHandler;
    private static NetBridgeService? s_instance;

    private volatile bool _isRunning;
    private volatile bool _disposed;

    public event Action<string>? LogReceived;

    public event Action<string, uint, string, ushort, string>? ConnectionReceived;

    public bool IsRunning => _isRunning;

    public NetBridgeService()
    {
        s_instance = this;
        s_staticLogHandler = StaticLogHandler;
        s_staticConnectionHandler = StaticConnectionHandler;

        NetBridgeNative.ProxyBridge_SetLogCallback(s_staticLogHandler);
        NetBridgeNative.ProxyBridge_SetConnectionCallback(s_staticConnectionHandler);
    }

    private static void StaticLogHandler(string message)
    {
        var self = s_instance;
        if (self is null || self._disposed) return;

        try
        {
            self.LogReceived?.Invoke(message);
        }
        catch
        {
        }
    }

    private static void StaticConnectionHandler(string processName, uint pid, string destIp, ushort destPort, string proxyInfo)
    {
        var self = s_instance;
        if (self is null || self._disposed) return;

        try
        {
            self.ConnectionReceived?.Invoke(processName, pid, destIp, destPort, proxyInfo);
        }
        catch
        {
        }
    }

    public bool Start()
    {
        if (_isRunning)
        {
            return true;
        }

        _isRunning = NetBridgeNative.ProxyBridge_Start();
        return _isRunning;
    }

    public bool Stop()
    {
        if (!_isRunning)
        {
            return true;
        }

        _isRunning = !NetBridgeNative.ProxyBridge_Stop();
        return !_isRunning;
    }

    public uint AddProxyConfig(string type, string ip, ushort port, string username, string password)
    {
        var proxyType = type.Equals("HTTP", StringComparison.OrdinalIgnoreCase) ? NetProxyType.HTTP : NetProxyType.SOCKS5;

        return NetBridgeNative.ProxyBridge_AddProxyConfig(proxyType, ip, port, username, password);
    }

    public bool EditProxyConfig(uint configId, string type, string ip, ushort port, string username, string password)
    {
        var proxyType = type.Equals("HTTP", StringComparison.OrdinalIgnoreCase) ? NetProxyType.HTTP : NetProxyType.SOCKS5;

        return NetBridgeNative.ProxyBridge_EditProxyConfig(configId, proxyType, ip, port, username, password);
    }

    public bool DeleteProxyConfig(uint configId)
    {
        return NetBridgeNative.ProxyBridge_DeleteProxyConfig(configId);
    }

    public string TestProxyConfig(uint configId, string targetHost, ushort targetPort)
    {
        var buffer = new System.Text.StringBuilder(4096);
        var result = NetBridgeNative.ProxyBridge_TestProxyConfig(configId, targetHost, targetPort, buffer, (UIntPtr)buffer.Capacity);
        if (result < 0)
        {
            return $"Test failed with error code: {result}";
        }
        return buffer.ToString();
    }

    public uint AddRule(string processName, string targetHosts, string targetPorts, string protocol, string action, uint proxyConfigId = 0)
    {
        var ruleAction = ParseRuleAction(action);
        var ruleProtocol = ParseRuleProtocol(protocol);

        return NetBridgeNative.ProxyBridge_AddRule(processName, targetHosts, targetPorts, ruleProtocol, ruleAction, proxyConfigId);
    }

    public bool EnableRule(uint ruleId)
    {
        return NetBridgeNative.ProxyBridge_EnableRule(ruleId);
    }

    public bool DisableRule(uint ruleId)
    {
        return NetBridgeNative.ProxyBridge_DisableRule(ruleId);
    }

    public bool DeleteRule(uint ruleId)
    {
        return NetBridgeNative.ProxyBridge_DeleteRule(ruleId);
    }

    public bool EditRule(uint ruleId, string processName, string targetHosts, string targetPorts, string protocol, string action, uint proxyConfigId = 0)
    {
        var ruleAction = ParseRuleAction(action);
        var ruleProtocol = ParseRuleProtocol(protocol);

        return NetBridgeNative.ProxyBridge_EditRule(ruleId, processName, targetHosts, targetPorts, ruleProtocol, ruleAction, proxyConfigId);
    }

    public uint GetRulePosition(uint ruleId)
    {
        return NetBridgeNative.ProxyBridge_GetRulePosition(ruleId);
    }

    public bool MoveRuleToPosition(uint ruleId, uint newPosition)
    {
        return NetBridgeNative.ProxyBridge_MoveRuleToPosition(ruleId, newPosition);
    }

    public void SetDnsViaProxy(bool enable)
    {
        NetBridgeNative.ProxyBridge_SetDnsViaProxy(enable);
    }

    public void SetLocalhostViaProxy(bool enable)
    {
        NetBridgeNative.ProxyBridge_SetLocalhostViaProxy(enable);
    }

    public static void SetTrafficLoggingEnabled(bool enable)
    {
        NetBridgeNative.ProxyBridge_SetTrafficLoggingEnabled(enable);
    }

    internal static NetRuleAction ParseRuleAction(string? action)
    {
        return action?.ToUpperInvariant() switch
        {
            "DIRECT" => NetRuleAction.DIRECT,
            "BLOCK" => NetRuleAction.BLOCK,
            "PROXY" => NetRuleAction.PROXY,
            _ => throw new ArgumentException($"Unknown rule action: '{action}'", nameof(action))
        };
    }

    internal static NetRuleProtocol ParseRuleProtocol(string? protocol)
    {
        return protocol?.ToUpperInvariant() switch
        {
            "TCP" => NetRuleProtocol.TCP,
            "UDP" => NetRuleProtocol.UDP,
            "BOTH" or "TCP+UDP" => NetRuleProtocol.BOTH,
            _ => throw new ArgumentException($"Unknown rule protocol: '{protocol}'", nameof(protocol))
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isRunning)
        {
            Stop();
        }

        LogReceived = null;
        ConnectionReceived = null;

        if (s_instance == this)
        {
            s_instance = null;
        }

        GC.SuppressFinalize(this);
    }
}
