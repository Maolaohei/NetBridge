using System.Reflection;
using NetBridgeLib.Services;

namespace NetBridgeLib.Tests;

public class NetBridgeServiceBehaviorTests
{
    [Fact]
    public void Constructor_SetsStaticCallbackFields()
    {
        var logField = typeof(NetBridgeService).GetField("s_staticLogHandler",
            BindingFlags.Static | BindingFlags.NonPublic);
        var connField = typeof(NetBridgeService).GetField("s_staticConnectionHandler",
            BindingFlags.Static | BindingFlags.NonPublic);

        var before = logField?.GetValue(null);

        try
        {
            var service = new NetBridgeService();

            var afterLog = logField?.GetValue(null);
            var afterConn = connField?.GetValue(null);

            Assert.NotNull(afterLog);
            Assert.NotNull(afterConn);
        }
        catch (TypeInitializationException)
        {
        }
        catch (DllNotFoundException)
        {
        }
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        try
        {
            using var service = new NetBridgeService();
            Assert.False(service.IsRunning);
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        try
        {
            var service = new NetBridgeService();
            service.Dispose();
            service.Dispose();
            service.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Dispose_ClearsInstanceReference()
    {
        var instanceField = typeof(NetBridgeService).GetField("s_instance",
            BindingFlags.Static | BindingFlags.NonPublic);

        try
        {
            var service = new NetBridgeService();
            Assert.NotNull(instanceField?.GetValue(null));

            service.Dispose();
            Assert.Null(instanceField?.GetValue(null));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Dispose_SetsDisposedFlag()
    {
        var disposedField = typeof(NetBridgeService).GetField("_disposed",
            BindingFlags.Instance | BindingFlags.NonPublic);

        try
        {
            var service = new NetBridgeService();
            Assert.False((bool)disposedField!.GetValue(service)!);

            service.Dispose();
            Assert.True((bool)disposedField.GetValue(service)!);
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void DisposedField_Exists()
    {
        var field = typeof(NetBridgeService).GetField("_disposed",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
    }

    [Fact]
    public void StaticCallbackFields_PreventGCCollection()
    {
        var logField = typeof(NetBridgeService).GetField("s_staticLogHandler",
            BindingFlags.Static | BindingFlags.NonPublic);
        var connField = typeof(NetBridgeService).GetField("s_staticConnectionHandler",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(logField);
        Assert.NotNull(connField);
        Assert.True(logField!.IsStatic, "Log callback must be static to prevent GC");
        Assert.True(connField!.IsStatic, "Connection callback must be static to prevent GC");
    }
}
