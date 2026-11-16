using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoneBox.Agent;

// PhoneBox Lite: a minimal, read-only host. Dangerous upstream routes and
// integrations are NOT registered. Run it manually; no installer or autostart.
var endpoint = AgentEndpoint.FromArgs(args);
if (args.Contains("--print-install-dir")) { Console.WriteLine(AppPaths.InstallDir); return; }

var selectedConfig = args.SkipWhile(a => a != "--config").Skip(1).FirstOrDefault();
var configPath = Path.GetFullPath(selectedConfig ?? Path.Combine(AppPaths.ConfigDir, "config.lite.json"));
if (!File.Exists(configPath))
{
    Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
    AppPaths.WriteAllTextPrivate(configPath,
        JsonSerializer.Serialize(LiteSafety.SanitizedConfig(null), ConfigStore.WriteOpts));
}
AppPaths.RestrictConfig(configPath);
var configStore = new ConfigStore(configPath);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, endpoint.Port));
builder.Logging.ClearProviders();
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddSingleton(configStore);
builder.Services.AddSingleton(endpoint);
builder.Services.AddSingleton<SystemMonitor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SystemMonitor>());
builder.Services.AddSingleton<AdbReverse>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdbReverse>());

var app = builder.Build();
string[] sameOrigins = [$"http://127.0.0.1:{endpoint.Port}", $"http://localhost:{endpoint.Port}"];

// The Android APK's /agent/api prefix is the same local API reached through adb reverse.
app.Use((ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/agent/api", out var rest))
        ctx.Request.Path = "/api" + rest;
    return next(ctx);
});

// Default deny for BOTH USB and local browser clients; no magic Android header
// or forged Origin can enable writes. Reject DNS rebinding and foreign browsers.
app.Use(async (ctx, next) =>
{
    if (!LiteSafety.Allows(ctx.Request.Method, ctx.Request.Path.Value ?? ""))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    var host = ctx.Request.Host.Host;
    var origin = ctx.Request.Headers.Origin.ToString();
    if ((host != "localhost" && host != "127.0.0.1")
        || (origin != "" && !sameOrigins.Contains(origin))
        || BrowserGuard.IsForeign(ctx.Request.Headers["Sec-Fetch-Site"], ctx.Request.Headers["Sec-Fetch-Mode"],
                                  ctx.Request.Method, ctx.Request.Path))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    ctx.Response.Headers.CacheControl = "no-store";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = ctx.Request.Path.StartsWithSegments("/panel") ? "SAMEORIGIN" : "DENY";
    await next();
});

// Editor remains visible for exploration, but settings are deliberately not writable.
app.MapGet("/", () =>
{
    using var stream = AppPaths.Resource("settings.html")!;
    using var reader = new StreamReader(stream);
    return Results.Content(reader.ReadToEnd(), "text/html; charset=utf-8");
});

app.MapGet("/panel/{*file}", (string? file, HttpRequest request) =>
{
    if (string.IsNullOrEmpty(file) && !request.Path.Value!.EndsWith('/'))
        return Results.Redirect("/panel/" + request.QueryString);
    file = string.IsNullOrEmpty(file) ? "index.html" : file;
    if (!Regex.IsMatch(file, @"^(widgets/)?[A-Za-z0-9_.-]+$") || file.Contains(".."))
        return Results.NotFound();
    var type = Path.GetExtension(file) switch
    {
        ".html" => "text/html; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        _ => "application/octet-stream",
    };
    return AppPaths.Resource("www/" + file) is { } asset
        ? Results.Stream(asset, type)
        : Results.NotFound();
});

app.MapGet("/favicon.ico", () =>
    AppPaths.Resource("phonebox.ico") is { } icon
        ? Results.Stream(icon, "image/x-icon")
        : Results.NotFound());

app.MapGet("/api/catalog", () => Results.Text(
    $"{{\"cols\":{Catalog.Cols},\"rows\":{Catalog.Rows},\"widgets\":{Catalog.WidgetsJson},\"templates\":{Catalog.TemplatesJson}}}",
    "application/json; charset=utf-8"));

app.MapGet("/api/settings", (ConfigStore config, HttpResponse response) =>
{
    response.Headers["X-Config-Version"] = config.Version.ToString();
    return Results.Json(LiteSafety.SanitizedConfig(config.Get()));
});

app.MapGet("/api/config", (ConfigStore config) =>
    Results.Json(new { version = config.Version, config = LiteSafety.SanitizedConfig(config.Get()).ToPublic() }));

app.MapGet("/api/info", (AdbReverse adb) => Results.Json(new
{
    version = AppPaths.Version,
    os = AppPaths.Os,
    connected = adb.Connected,
    via = adb.Connected ? "USB" : null,
}));

app.MapGet("/api/state", (SystemMonitor monitor, ConfigStore config) =>
{
    var snapshot = monitor.Snapshot();
    return Results.Json(new
    {
        ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        cfgVersion = config.Version,
        pc = snapshot is null ? null : new
        {
            snapshot.Cpu,
            snapshot.RamUsedGb,
            snapshot.RamTotalGb,
            snapshot.NetDownBps,
            snapshot.NetUpBps,
            snapshot.Disks,
            snapshot.UptimeSec,
        },
    });
});

app.Logger.LogInformation("PhoneBox Lite read-only USB host on http://127.0.0.1:{Port}", endpoint.Port);
app.Run();
