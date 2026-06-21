using System.Diagnostics;
using NetBridge.Desktop.ViewModels;
using NetBridge.Desktop.Views;

namespace NetBridge.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnExit;
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject != null)
        {
            Debug.WriteLine("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine("TaskScheduler_UnobservedTaskException", e.Exception);
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (sender is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                if (desktop.MainWindow?.DataContext is MainWindowViewModel vm)
                {
                    if (vm.IsProxyRunning)
                    {
                        vm.Stop();
                    }
                    vm.ProxyService?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnExit cleanup error: {ex.Message}");
            }
        }
    }

    private void MenuExit_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
