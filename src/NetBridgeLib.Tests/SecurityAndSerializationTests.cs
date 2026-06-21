using System.Text.Json;

namespace NetBridgeLib.Tests;

public class SecurityAndSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class ProxyConfigModel
    {
        public uint ProxyConfigId { get; set; }
        public string ProxyType { get; set; } = "SOCKS5";
        public string ProxyHost { get; set; } = "127.0.0.1";
        public ushort ProxyPort { get; set; } = 10000;
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;
    }

    private sealed class RuleConfigModel
    {
        public uint RuleId { get; set; }
        public string ProcessName { get; set; } = "Chrome.exe";
        public string TargetHosts { get; set; } = "*";
        public string TargetPorts { get; set; } = "*";
        public string Protocol { get; set; } = "TCP";
        public string Action { get; set; } = "PROXY";
        public uint ProxyConfigId { get; set; }
    }

    private sealed class AppSettingsModel
    {
        public ProxyConfigModel ProxyConfig { get; set; } = new();
        public List<RuleConfigModel> Rules { get; set; } = [];
    }

    [Fact]
    public void Password_SerializedAsPlaintext()
    {
        var config = new ProxyConfigModel { ProxyPassword = "MySecret123!" };
        var json = JsonSerializer.Serialize(config, JsonOptions);

        Assert.Contains("MySecret123!", json);
    }

    [Fact]
    public void Password_Roundtrip_PreservesExactValue()
    {
        var original = new ProxyConfigModel
        {
            ProxyPassword = "p@ssw0rd!#$%^&*()"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProxyConfigModel>(json, JsonOptions);

        Assert.Equal(original.ProxyPassword, deserialized!.ProxyPassword);
    }

    [Fact]
    public void EmptyPassword_SerializesAsEmptyString()
    {
        var config = new ProxyConfigModel { ProxyPassword = "" };
        var json = JsonSerializer.Serialize(config, JsonOptions);

        Assert.Contains("\"ProxyPassword\": \"\"", json);
    }

    [Fact]
    public void SpecialCharactersInPassword_PreservedThroughRoundtrip()
    {
        var passwords = new[]
        {
            "简单密码",
            "パスワード",
            "🔐🔑",
            "with\nnewlines",
            "with\"quotes",
            "with\\backslash",
            new string('a', 10000),
        };

        foreach (var password in passwords)
        {
            var config = new ProxyConfigModel { ProxyPassword = password };
            var json = JsonSerializer.Serialize(config, JsonOptions);
            var deserialized = JsonSerializer.Deserialize<ProxyConfigModel>(json, JsonOptions);
            Assert.Equal(password, deserialized!.ProxyPassword);
        }
    }

    [Fact]
    public void PortBoundary_Zero()
    {
        var config = new ProxyConfigModel { ProxyPort = 0 };
        Assert.Equal((ushort)0, config.ProxyPort);
    }

    [Fact]
    public void PortBoundary_MaxValue()
    {
        var config = new ProxyConfigModel { ProxyPort = ushort.MaxValue };
        Assert.Equal(65535, config.ProxyPort);
    }

    [Fact]
    public void PortBoundary_CommonValues()
    {
        var ports = new ushort[] { 80, 443, 1080, 8080, 10000, 65535 };
        foreach (var port in ports)
        {
            var config = new ProxyConfigModel { ProxyPort = port };
            var json = JsonSerializer.Serialize(config, JsonOptions);
            var deserialized = JsonSerializer.Deserialize<ProxyConfigModel>(json, JsonOptions);
            Assert.Equal(port, deserialized!.ProxyPort);
        }
    }

    [Fact]
    public void ConfigRoundtrip_AllFieldsPreserved()
    {
        var settings = new AppSettingsModel
        {
            ProxyConfig = new ProxyConfigModel
            {
                ProxyConfigId = 42,
                ProxyType = "HTTP",
                ProxyHost = "proxy.example.com",
                ProxyPort = 8080,
                ProxyUsername = "user",
                ProxyPassword = "pass"
            },
            Rules =
            [
                new RuleConfigModel
                {
                    RuleId = 1,
                    ProcessName = "Chrome.exe",
                    TargetHosts = "*.google.com",
                    TargetPorts = "443",
                    Protocol = "TCP",
                    Action = "PROXY",
                    ProxyConfigId = 42
                },
                new RuleConfigModel
                {
                    RuleId = 2,
                    ProcessName = "Firefox.exe",
                    TargetHosts = "*",
                    TargetPorts = "*",
                    Protocol = "BOTH",
                    Action = "DIRECT",
                    ProxyConfigId = 0
                }
            ]
        };

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppSettingsModel>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(42u, deserialized.ProxyConfig.ProxyConfigId);
        Assert.Equal("HTTP", deserialized.ProxyConfig.ProxyType);
        Assert.Equal("proxy.example.com", deserialized.ProxyConfig.ProxyHost);
        Assert.Equal((ushort)8080, deserialized.ProxyConfig.ProxyPort);
        Assert.Equal(2, deserialized.Rules.Count);
        Assert.Equal("Chrome.exe", deserialized.Rules[0].ProcessName);
        Assert.Equal("DIRECT", deserialized.Rules[1].Action);
    }

    [Fact]
    public void NullJson_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<AppSettingsModel>("null", JsonOptions);
        Assert.Null(result);
    }

    [Fact]
    public void CorruptedJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<AppSettingsModel>("not valid json", JsonOptions));
    }

    [Fact]
    public void EmptyJson_ReturnsObjectWithDefaults()
    {
        var result = JsonSerializer.Deserialize<AppSettingsModel>("{}", JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("SOCKS5", result.ProxyConfig.ProxyType);
        Assert.Equal("127.0.0.1", result.ProxyConfig.ProxyHost);
        Assert.Equal((ushort)10000, result.ProxyConfig.ProxyPort);
        Assert.Empty(result.Rules);
    }

    [Fact]
    public void MissingFields_UseDefaults()
    {
        var json = "{\"ProxyConfig\":{}}";
        var result = JsonSerializer.Deserialize<AppSettingsModel>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal("SOCKS5", result.ProxyConfig.ProxyType);
        Assert.Equal((ushort)10000, result.ProxyConfig.ProxyPort);
    }

    [Fact]
    public void ListSerialization_NotObservableCollection()
    {
        var settings = new AppSettingsModel
        {
            Rules = [new RuleConfigModel { RuleId = 1, ProcessName = "test.exe" }]
        };

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppSettingsModel>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.IsType<List<RuleConfigModel>>(deserialized.Rules);
    }
}
