namespace NetBridgeLib.Tests;

public class ProcessNameParsingTests
{
    private static List<string> ParseProcessNames(string input)
    {
        return [.. input
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    [Fact]
    public void SingleProcess_ReturnsList()
    {
        var result = ParseProcessNames("Chrome.exe");
        Assert.Single(result);
        Assert.Equal("Chrome.exe", result[0]);
    }

    [Fact]
    public void MultipleProcesses_SplitsByComma()
    {
        var result = ParseProcessNames("Chrome.exe,Firefox.exe,Edge.exe");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void TrimsWhitespace()
    {
        var result = ParseProcessNames("  Chrome.exe  ,  Firefox.exe  ");
        Assert.Equal(2, result.Count);
        Assert.Equal("Chrome.exe", result[0]);
        Assert.Equal("Firefox.exe", result[1]);
    }

    [Fact]
    public void RemovesDuplicates_CaseInsensitive()
    {
        var result = ParseProcessNames("Chrome.exe,chrome.exe,CHROME.EXE");
        Assert.Single(result);
    }

    [Fact]
    public void EmptyString_ReturnsEmptyList()
    {
        var result = ParseProcessNames("");
        Assert.Empty(result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsEmptyList()
    {
        var result = ParseProcessNames("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void OnlyCommas_ReturnsEmptyList()
    {
        var result = ParseProcessNames(",,,");
        Assert.Empty(result);
    }

    [Fact]
    public void EmptySegmentsBetweenCommas_AreIgnored()
    {
        var result = ParseProcessNames("Chrome.exe,,Firefox.exe,");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void PreservesCaseOfFirstOccurrence()
    {
        var result = ParseProcessNames("chrome.exe,Chrome.exe");
        Assert.Single(result);
        Assert.Equal("chrome.exe", result[0]);
    }

    [Fact]
    public void ProcessNameWithSpaces_IsTrimmed()
    {
        var result = ParseProcessNames(" My App.exe ");
        Assert.Single(result);
        Assert.Equal("My App.exe", result[0]);
    }

    [Fact]
    public void SingleProcessNoComma_Works()
    {
        var result = ParseProcessNames("notepad.exe");
        Assert.Single(result);
        Assert.Equal("notepad.exe", result[0]);
    }

    [Fact]
    public void ManyProcesses_HandledCorrectly()
    {
        var input = string.Join(",", Enumerable.Range(1, 100).Select(i => $"proc{i}.exe"));
        var result = ParseProcessNames(input);
        Assert.Equal(100, result.Count);
    }

    [Fact]
    public void UnicodeProcessName_Preserved()
    {
        var result = ParseProcessNames("测试.exe");
        Assert.Single(result);
        Assert.Equal("测试.exe", result[0]);
    }

    [Fact]
    public void SpecialCharactersInName_Preserved()
    {
        var result = ParseProcessNames("my-app (v2).exe");
        Assert.Single(result);
        Assert.Equal("my-app (v2).exe", result[0]);
    }
}
