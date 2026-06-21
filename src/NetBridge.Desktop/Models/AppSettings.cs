namespace NetBridge.Desktop.Models;

public sealed class AppSettings
{
    public ProxyConfig ProxyConfig { get; set; } = new();
    public List<RuleConfig> Rules { get; set; } = [];
}
