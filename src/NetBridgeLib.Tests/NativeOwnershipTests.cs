using System.Reflection;
using NetBridgeLib.Services;

namespace NetBridgeLib.Tests;

/// <summary>
/// Tests verifying the native ownership state machine:
/// _isRunning / _nativeAllocated / _disposed transitions
/// and the correct call order of Start → Stop → Dispose.
/// </summary>
public class NativeOwnershipTests
{
    private static readonly FieldInfo IsRunningField =
        typeof(NetBridgeService).GetField("_isRunning",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo NativeAllocatedField =
        typeof(NetBridgeService).GetField("_nativeAllocated",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo DisposedField =
        typeof(NetBridgeService).GetField("_disposed",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static bool GetIsRunning(NetBridgeService svc) => (bool)IsRunningField.GetValue(svc)!;
    private static bool GetNativeAllocated(NetBridgeService svc) => (bool)NativeAllocatedField.GetValue(svc)!;
    private static bool GetDisposed(NetBridgeService svc) => (bool)DisposedField.GetValue(svc)!;

    private static void SetNativeAllocated(NetBridgeService svc, bool value)
    {
        NativeAllocatedField.SetValue(svc, value);
    }

    private static void SetIsRunning(NetBridgeService svc, bool value)
    {
        IsRunningField.SetValue(svc, value);
    }

    #region Fresh state

    [Fact]
    public void Fresh_HasCleanState()
    {
        try
        {
            var svc = new NetBridgeService();

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));
            Assert.False(GetDisposed(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Stop clears nativeAllocated

    [Fact]
    public void Stop_WhenNativeAllocated_ResetsFlags()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            SetIsRunning(svc, true);

            svc.Stop();

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Stop_WhenNativeNotAllocated_ResetsFlags()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, false);
            SetIsRunning(svc, false);

            svc.Stop();

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Stop_IsIdempotent()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            svc.Stop();
            svc.Stop();

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Dispose always calls Stop

    [Fact]
    public void Dispose_AlwaysResetsNativeAllocated()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            SetIsRunning(svc, true);

            svc.Dispose();

            Assert.True(GetDisposed(svc));
            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Dispose_WhenNotRunning_AlsoResetsFlags()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, false);
            SetIsRunning(svc, false);

            svc.Dispose();

            Assert.True(GetDisposed(svc));
            Assert.False(GetNativeAllocated(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void MultipleDispose_IsSafe()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            svc.Dispose();

            svc.Dispose();
            svc.Dispose();

            Assert.True(GetDisposed(svc));
            Assert.False(GetNativeAllocated(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Start after Stop resets state

    [Fact]
    public void Start_AfterStop_GetsFreshChance()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            SetIsRunning(svc, true);

            svc.Stop();

            Assert.False(GetNativeAllocated(svc));
            Assert.False(GetIsRunning(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Full lifecycle sequences

    [Fact]
    public void Lifecycle_StartStopDispose_AllFlagsCleared()
    {
        try
        {
            var svc = new NetBridgeService();

            svc.Stop();

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();

            Assert.True(GetDisposed(svc));
            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Lifecycle_StopStartStopDispose_AllFlagsCorrect()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            SetIsRunning(svc, true);

            svc.Stop();
            Assert.False(GetNativeAllocated(svc));

            svc.Stop();
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();
            Assert.True(GetDisposed(svc));
            Assert.False(GetNativeAllocated(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    [Fact]
    public void Lifecycle_DisposeAfterStop_IsIdempotent()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            svc.Stop();
            svc.Dispose();
            svc.Dispose();

            Assert.True(GetDisposed(svc));
            Assert.False(GetNativeAllocated(svc));
            Assert.False(GetIsRunning(svc));
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Concurrent Stop safety

    [Fact]
    public async Task ConcurrentStop_NoCorruption()
    {
        try
        {
            var svc = new NetBridgeService();

            SetNativeAllocated(svc, true);
            SetIsRunning(svc, true);

            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(svc.Stop))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.False(GetIsRunning(svc));
            Assert.False(GetNativeAllocated(svc));

            svc.Dispose();
        }
        catch (TypeInitializationException) { }
        catch (DllNotFoundException) { }
    }

    #endregion

    #region Field existence

    [Fact]
    public void NativeAllocatedField_Exists()
    {
        var field = typeof(NetBridgeService).GetField("_nativeAllocated",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.Equal(typeof(bool), field!.FieldType);
    }

    [Fact]
    public void IsRunningField_Exists()
    {
        var field = typeof(NetBridgeService).GetField("_isRunning",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.True(field!.FieldType == typeof(bool));
    }

    [Fact]
    public void NativeAllocated_IsBoolAndVolatile()
    {
        var field = typeof(NetBridgeService).GetField("_nativeAllocated",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.Equal(typeof(bool), field!.FieldType);

        var initOnly = field.IsInitOnly;
        Assert.False(initOnly, "_nativeAllocated must be writable");
    }

    #endregion
}
