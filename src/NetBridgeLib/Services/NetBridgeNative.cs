using System.Runtime.InteropServices;
using NetBridgeLib.Enums;

namespace NetBridgeLib.Services;

public static class NetBridgeNative
{
    private const string DllName = "ProxyBridgeCore.dll";
    private static bool s_dllLoaded;

    static NetBridgeNative()
    {
        var assemblyPath = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            var dllPath = Path.Combine(assemblyPath, "bin", "NetBridge", DllName);
            try
            {
                if (File.Exists(dllPath))
                {
                    NativeLibrary.Load(dllPath);
                    s_dllLoaded = true;
                }
                else
                {
                    Console.Error.WriteLine($"[NetBridge] DLL not found: {dllPath}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NetBridge] Failed to load DLL: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Returns true if the native DLL was loaded successfully.
    /// </summary>
    public static bool IsDllLoaded => s_dllLoaded;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ConnectionCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string processName,
        uint pid,
        [MarshalAs(UnmanagedType.LPStr)] string destIp,
        ushort destPort,
        [MarshalAs(UnmanagedType.LPStr)] string proxyInfo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_AddRule(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string processName,
        [MarshalAs(UnmanagedType.LPStr)] string targetHosts,
        [MarshalAs(UnmanagedType.LPStr)] string targetPorts,
        NetRuleProtocol protocol,
        NetRuleAction action,
        uint proxyConfigId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_EnableRule(uint ruleId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_DisableRule(uint ruleId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_DeleteRule(uint ruleId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_EditRule(
        uint ruleId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string processName,
        [MarshalAs(UnmanagedType.LPStr)] string targetHosts,
        [MarshalAs(UnmanagedType.LPStr)] string targetPorts,
        NetRuleProtocol protocol,
        NetRuleAction action,
        uint proxyConfigId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_GetRulePosition(uint ruleId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_MoveRuleToPosition(uint ruleId, uint newPosition);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_AddProxyConfig(
        NetProxyType type,
        [MarshalAs(UnmanagedType.LPStr)] string proxyIp,
        ushort proxyPort,
        [MarshalAs(UnmanagedType.LPStr)] string username,
        [MarshalAs(UnmanagedType.LPStr)] string password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_EditProxyConfig(
        uint configId,
        NetProxyType type,
        [MarshalAs(UnmanagedType.LPStr)] string proxyIp,
        ushort proxyPort,
        [MarshalAs(UnmanagedType.LPStr)] string username,
        [MarshalAs(UnmanagedType.LPStr)] string password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_DeleteProxyConfig(uint configId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int ProxyBridge_TestProxyConfig(
        uint configId,
        [MarshalAs(UnmanagedType.LPStr)] string targetHost,
        ushort targetPort,
        [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder resultBuffer,
        UIntPtr bufferSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyBridge_SetLogCallback(LogCallback callback);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyBridge_SetConnectionCallback(ConnectionCallback callback);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyBridge_SetTrafficLoggingEnabled([MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyBridge_SetDnsViaProxy([MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyBridge_SetLocalhostViaProxy([MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_Start();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ProxyBridge_Stop();

    // v2.1.0 Phase 2: Version and diagnostics APIs

    /// <summary>
    /// Returns the native DLL version as a packed integer (major&lt;&lt;16 | minor&lt;&lt;8 | patch).
    /// Returns 0 if the function is not available (old DLL version).
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_GetVersion();

    /// <summary>
    /// Returns the last error code from the native layer.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ProxyBridge_GetLastError();

    /// <summary>
    /// Returns the number of active connections being proxied.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_GetConnectionCount();

    /// <summary>
    /// Returns the number of active UDP sessions.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyBridge_GetSessionCount();
}
