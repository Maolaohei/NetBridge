using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using Avalonia.Threading;
using NetBridge.Desktop.Common;
using NetBridge.Desktop.Models;
using NetBridge.Desktop.Services;
using NetBridgeLib.Services;
using ReactiveUI.Fody.Helpers;

namespace NetBridge.Desktop.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> ToggleProxyCmd { get; }
    public ReactiveCommand<Unit, Unit> ApplyProxyConfigCmd { get; }
    public ReactiveCommand<Unit, Unit> SaveRuleCmd { get; }

    public ObservableCollection<string> Logs { get; } = [];

    [Reactive]
    public ProxyConfig ProxyConfigSource { get; set; } = new();

    [Reactive]
    public string RuleProcessName { get; set; } = "Chrome.exe";

    [Reactive]
    public string RuleProtocol { get; set; } = "TCP";

    public IReadOnlyList<string> ProtocolOptions { get; } = ["TCP", "UDP", "BOTH"];

    public bool IsProxyRunning { get; set; }

    [Reactive]
    public string ToggleServiceButtonText { get; set; } = "启动";

    public NetBridgeService? ProxyService { get; private set; }
    private readonly AppSettingsStorage _proxyConfigStorage = new();

    public MainWindowViewModel()
    {
        ToggleProxyCmd = ReactiveCommand.Create(() =>
        {
            if (IsProxyRunning)
            {
                Stop();
            }
            else
            {
                Start();
            }

            ToggleServiceButtonText = IsProxyRunning ? "关闭" : "启动";
        });

        ApplyProxyConfigCmd = ReactiveCommand.Create(() =>
        {
            ApplyProxyConfig();
        });

        SaveRuleCmd = ReactiveCommand.Create(() =>
        {
            SaveRule();
        });

        try
        {
            ProxyConfigSource = _proxyConfigStorage.LoadProxyConfig();
            var savedRules = _proxyConfigStorage.LoadRules();
            RuleProcessName = string.Join(',', savedRules.Select(r => r.ProcessName).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase));
            var savedProtocol = savedRules.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Protocol))?.Protocol;
            if (!string.IsNullOrWhiteSpace(savedProtocol))
            {
                RuleProtocol = savedProtocol;
            }

            ProxyService = new NetBridgeService();
            ProxyService.LogReceived += msg =>
            {
                Debug.WriteLine(msg);
                AppendLog(msg);
            };

            ProxyService.ConnectionReceived += (processName, pid, destIp, destPort, proxyInfo) =>
            {
                var message = $"Connection: {processName} (PID: {pid}) -> {destIp}:{destPort} via {proxyInfo}";
                Debug.WriteLine(message);
                AppendLog(message);
            };

            ApplyProxyConfig();
            ApplyRulesFromInput();
            AppendLog("代理服务初始化完成。已读取并应用本地代理配置与规则。");
        }
        catch (Exception ex)
        {
            var error = $"Failed to initialize ProxyService: {ex.Message}";
            Debug.WriteLine(error);
            AppendLog(error);
        }
    }

    public bool Start()
    {
        if (!AvaUtils.IsAdministrator())
        {
            AppendLog("启动失败：请以管理员权限运行应用。");
            return false;
        }

        try
        {
            ApplyProxyConfig();
            if (ProxyService?.Start() != true)
            {
                AppendLog("代理服务启动失败。");
                return false;
            }
            IsProxyRunning = true;
            AppendLog("代理服务已启动。");
        }
        catch (Exception ex)
        {
            var error = $"Failed to start ProxyService: {ex.Message}";
            Debug.WriteLine(error);
            AppendLog(error);
            return false;
        }

        return true;
    }

    public bool Stop()
    {
        if (!AvaUtils.IsAdministrator())
        {
            AppendLog("停止失败：请以管理员权限运行应用。");
            return false;
        }

        if (ProxyService?.Stop() == false)
        {
            AppendLog("代理服务停止失败。");
            return false;
        }
        IsProxyRunning = false;
        AppendLog("代理服务已停止。");
        return true;
    }

    private void ApplyProxyConfig()
    {
        if (ProxyService is null)
        {
            _proxyConfigStorage.SaveProxyConfig(ProxyConfigSource);
            AppendLog("代理服务未初始化，仅保存本地代理配置。");
            return;
        }

        if (ProxyConfigSource.ProxyConfigId == 0)
        {
            var newId = ProxyService.AddProxyConfig(ProxyConfigSource.ProxyType, ProxyConfigSource.ProxyHost, ProxyConfigSource.ProxyPort, ProxyConfigSource.ProxyUsername, ProxyConfigSource.ProxyPassword);
            ProxyConfigSource.ProxyConfigId = newId;
        }
        else
        {
            var edited = ProxyService.EditProxyConfig(ProxyConfigSource.ProxyConfigId, ProxyConfigSource.ProxyType, ProxyConfigSource.ProxyHost, ProxyConfigSource.ProxyPort, ProxyConfigSource.ProxyUsername, ProxyConfigSource.ProxyPassword);
            if (!edited)
            {
                var newId = ProxyService.AddProxyConfig(ProxyConfigSource.ProxyType, ProxyConfigSource.ProxyHost, ProxyConfigSource.ProxyPort, ProxyConfigSource.ProxyUsername, ProxyConfigSource.ProxyPassword);
                ProxyConfigSource.ProxyConfigId = newId;
            }
        }

        _proxyConfigStorage.SaveProxyConfig(ProxyConfigSource);
        AppendLog($"已应用并保存代理配置(ID: {ProxyConfigSource.ProxyConfigId}): {ProxyConfigSource.ProxyType} {ProxyConfigSource.ProxyHost}:{ProxyConfigSource.ProxyPort}");
    }

    private void SaveRule()
    {
        ApplyRulesFromInput();
    }

    private void ApplyRulesFromInput()
    {
        if (ProxyService is null)
        {
            return;
        }

        var processNames = ParseProcessNames(RuleProcessName);
        if (processNames.Count == 0)
        {
            var oldRules = _proxyConfigStorage.LoadRules();
            foreach (var oldRule in oldRules)
            {
                ProxyService.DeleteRule(oldRule.RuleId);
            }
            _proxyConfigStorage.SaveRules([]);
            RuleProcessName = string.Empty;
            AppendLog("已清空演示规则。");
            return;
        }

        var newRules = new List<RuleConfig>();
        foreach (var processName in processNames)
        {
            var newRuleId = ProxyService.AddRule(processName, "*", "*", RuleProtocol, "PROXY");
            if (newRuleId == 0)
            {
                AppendLog($"添加规则失败：{processName}，已添加的规则将保留。");
                continue;
            }
            newRules.Add(new RuleConfig
            {
                RuleId = newRuleId,
                ProcessName = processName,
                TargetHosts = "*",
                TargetPorts = "*",
                Protocol = RuleProtocol,
                Action = "PROXY",
                ProxyConfigId = 0
            });
        }

        if (newRules.Count > 0)
        {
            var oldRules = _proxyConfigStorage.LoadRules();
            foreach (var oldRule in oldRules)
            {
                ProxyService.DeleteRule(oldRule.RuleId);
            }
            _proxyConfigStorage.SaveRules(newRules);
            RuleProcessName = string.Join(',', newRules.Select(r => r.ProcessName));
            AppendLog($"已保存演示规则，共 {newRules.Count} 条。");
        }
    }

    internal static List<string> ParseProcessNames(string input)
    {
        return [.. input
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.Post(() => Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}"));
    }
}
