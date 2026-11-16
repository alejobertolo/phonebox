using System.Text.Json;
using PhoneBox.Agent;
using Xunit;

public sealed class LiteSafetyTests
{
    [Theory]
    [InlineData("GET", "/", true)]
    [InlineData("GET", "/panel/", true)]
    [InlineData("GET", "/panel/main.js", true)]
    [InlineData("GET", "/api/state", true)]
    [InlineData("GET", "/api/stream", false)]
    [InlineData("GET", "/api/config", true)]
    [InlineData("GET", "/api/catalog", true)]
    [InlineData("GET", "/api/settings", true)]
    [InlineData("GET", "/api/info", true)]
    [InlineData("GET", "/api/proxy?url=https://example.com", false)]
    [InlineData("GET", "/api/widget/page/widget", false)]
    [InlineData("GET", "/api/controls/spotify/connect", false)]
    [InlineData("GET", "/api/work", false)]
    [InlineData("GET", "/api/update", false)]
    [InlineData("GET", "/api/lan", false)]
    [InlineData("GET", "/api/unknown", false)]
    [InlineData("GET", "/panel-evil/main.js", false)]
    [InlineData("POST", "/api/controls/command/volume", false)]
    [InlineData("POST", "/api/shortcuts/test", false)]
    [InlineData("POST", "/api/apps/open", false)]
    [InlineData("POST", "/api/procs/kill", false)]
    [InlineData("POST", "/api/update/apply", false)]
    [InlineData("POST", "/api/run/terminal", false)]
    [InlineData("POST", "/api/notify", false)]
    [InlineData("POST", "/api/pair/claim", false)]
    [InlineData("POST", "/api/approvals/id", false)]
    [InlineData("PUT", "/api/settings", false)]
    [InlineData("DELETE", "/api/push", false)]
    [InlineData("HEAD", "/api/state", false)]
    public void OnlyTheReviewedReadOnlySurfaceIsReachable(string method, string path, bool expected) =>
        Assert.Equal(expected, LiteSafety.Allows(method, path));

    [Fact]
    public void PhoneConfigurationIsReducedToOneSafeSystemPage()
    {
        var original = new AgentConfig
        {
            Lan = new LanConfig { Enabled = true, Token = "secret" },
            Updates = new UpdateConfig { Auto = true },
            Claude = new ClaudeConfig { PhoneApprovals = true },
            Display = new DisplayConfig { HomeScreen = true, KeepAwake = true },
            Pages = [new PageConfig { Title = "Unsafe", Widgets = [new WidgetConfig { Type = "command" }] }],
        };

        var safe = LiteSafety.SanitizedConfig(original);
        Assert.False(safe.Lan.Enabled);
        Assert.Null(safe.Lan.Token);
        Assert.False(safe.Updates.Auto);
        Assert.False(safe.Claude.PhoneApprovals);
        Assert.False(safe.Display.HomeScreen);
        Assert.False(safe.Display.KeepAwake);
        Assert.Empty(safe.Shortcuts);
        Assert.Empty(safe.Rules);
        var page = Assert.Single(safe.Pages);
        Assert.Equal("system", page.Id);
        Assert.All(page.Widgets!, w => Assert.Contains(w.Type, new[] { "chart", "meters", "stat", "gauge" }));
    }

    [Fact]
    public void LiteUsesIndependentApplicationSupportDirectory()
    {
        Assert.Equal("PhoneBoxLite", AppPaths.AppName);
        Assert.Equal("PhoneBoxLite", Path.GetFileName(AppPaths.ConfigDir));
        Assert.Equal("PhoneBoxLite", Path.GetFileName(AppPaths.LocalDir));
    }

    [Fact]
    public void LiteUsesIndependentApplicationSupportDirectory()
    {
        Assert.Equal("PhoneBoxLite", AppPaths.AppName);
        Assert.Equal("PhoneBoxLite", Path.GetFileName(AppPaths.ConfigDir));
        Assert.Equal("PhoneBoxLite", Path.GetFileName(AppPaths.LocalDir));
    }

    [Fact]
    public void ShippedCatalogExposesNoActiveControlsOrExternalFeeds()
    {
        using var document = JsonDocument.Parse(Catalog.WidgetsJson);
        var allowed = new[] { "chart", "meters", "stat", "gauge" };
        foreach (var w in document.RootElement.EnumerateArray())
            Assert.Contains(w.GetProperty("type").GetString(), allowed);

        var template = Assert.Single(Catalog.Templates);
        Assert.Equal("system", template.Id);
    }
}
