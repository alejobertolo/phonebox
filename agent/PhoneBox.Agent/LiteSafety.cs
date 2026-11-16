using System.Text.Json;

namespace PhoneBox.Agent;

/// <summary>
/// PhoneBox Lite v1 is a USB-only, telemetry-only build. This is a hard boundary,
/// not a feature flag: no remote actions, settings writes or network proxy.
/// Keep the allowlist small and review every addition before shipping.
/// </summary>
public static class LiteSafety
{
    private static readonly HashSet<string> ApiReads = new(StringComparer.Ordinal)
    {
        "/api/state", "/api/config", "/api/catalog", "/api/settings", "/api/info",
    };

    private static readonly HashSet<string> SafeWidgets = new(StringComparer.Ordinal)
    {
        "chart", "meters", "stat", "gauge",
    };

    public static bool Allows(string method, string pathAndQuery)
    {
        if (method != "GET" || string.IsNullOrWhiteSpace(pathAndQuery)) return false;
        var path = pathAndQuery.Split('?', 2)[0];
        if (path is "/" or "/favicon.ico" or "/panel") return true;
        if (ApiReads.Contains(path)) return true;
        return path.StartsWith("/panel/", StringComparison.Ordinal)
            && !path.Contains("..", StringComparison.Ordinal)
            && !path.Contains('\\');
    }

    /// <summary>Only passive PC telemetry widgets are advertised to the editor.</summary>
    public static string ReadOnlyWidgetsJson(string original)
    {
        using var doc = JsonDocument.Parse(original);
        return JsonSerializer.Serialize(doc.RootElement.EnumerateArray()
            .Where(widget => SafeWidgets.Contains(widget.GetProperty("type").GetString() ?? ""))
            .Select(widget => widget.Clone()).ToArray());
    }

    /// <summary>Only a safe system layout is available in Lite. Validate upstream JSON first.</summary>
    public static string ReadOnlyTemplatesJson(string original)
    {
        using var _ = JsonDocument.Parse(original);
        return JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "system",
                title = "System",
                icon = "📊",
                desc = "CPU, RAM, disk and network · read-only USB dashboard",
                widgets = new object[]
                {
                    new { type = "chart", x = 0, y = 0, w = 6, h = 3, options = new { metrics = new[] { "cpu" } } },
                    new { type = "chart", x = 6, y = 0, w = 6, h = 3, options = new { metrics = new[] { "down", "up" } } },
                    new { type = "meters", x = 0, y = 3, w = 8, h = 3 },
                    new { type = "stat", x = 8, y = 3, w = 4, h = 3, options = new { metric = "ram" } },
                },
            },
        });
    }

    /// <summary>
    /// Do not trust any settings from a preexisting PhoneBox installation.
    /// The mobile panel and read-only editor always receive this fresh allowlisted config.
    /// </summary>
    public static AgentConfig SanitizedConfig(AgentConfig? original)
    {
        return new AgentConfig
        {
            DeviceSerial = original?.DeviceSerial,
            // Never take an executable adb path or action configuration from disk.
            AdbPath = null,
            Lan = new LanConfig { Enabled = false },
            Updates = new UpdateConfig { Auto = false, Repo = "" },
            Claude = new ClaudeConfig { PhoneApprovals = false },
            Ping = new PingConfig { Host = null },
            Night = new NightConfig { Enabled = false },
            Display = new DisplayConfig
            {
                Orientation = "landscape",
                KeepAwake = false,
                HomeScreen = false,
                FollowComputerLock = false,
                AutoBrightness = false,
                PixelShift = true,
                IdleDimMin = 15,
            },
            Pages = new List<PageConfig>
            {
                new()
                {
                    Id = "system",
                    Title = "System",
                    Icon = "📊",
                    Enabled = true,
                    Widgets = Catalog.Template("system")!.Widgets.Select(widget => widget.Clone()).ToList(),
                },
            },
            Shortcuts = [],
            Rules = [],
            Calendars = [],
            Phones = [],
        };
    }
}
