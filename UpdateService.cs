using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BdoCronCalculator;

public class UpdateInfo
{
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string? ExeDownloadUrl { get; set; }
    public string? ApkDownloadUrl { get; set; }
    public string ReleaseUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
}

public class UpdateService
{
    private const string GitHubApiLatestReleaseUrl = "https://api.github.com/repos/Pravix212/BdoCronCalculator/releases/latest";
    private static readonly HttpClient _httpClient = new();

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BdoCronCalculator-App/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public static Version GetCurrentVersion()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        return ver ?? new Version(1, 3, 4);
    }

    public static async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiLatestReleaseUrl);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagProp))
            {
                return null;
            }

            string tagName = tagProp.GetString() ?? string.Empty;
            string cleanTag = tagName.TrimStart('v', 'V');

            if (!Version.TryParse(cleanTag, out var remoteVersion))
            {
                if (cleanTag.Contains('.'))
                {
                    var parts = cleanTag.Split('.');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
                    {
                        remoteVersion = new Version(major, minor, 0);
                    }
                }
            }

            var currentVersion = GetCurrentVersion();
            bool isNewer = remoteVersion != null && remoteVersion > currentVersion;

            string? exeUrl = null;
            string? apkUrl = null;

            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameProp) && asset.TryGetProperty("browser_download_url", out var urlProp))
                    {
                        string name = nameProp.GetString() ?? string.Empty;
                        string url = urlProp.GetString() ?? string.Empty;

                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            exeUrl = url;
                        }
                        else if (name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                        {
                            apkUrl = url;
                        }
                    }
                }
            }

            string releaseUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? string.Empty : string.Empty;
            string notes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;

            return new UpdateInfo
            {
                HasUpdate = isNewer,
                CurrentVersion = $"v{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}",
                LatestVersion = tagName,
                ExeDownloadUrl = exeUrl,
                ApkDownloadUrl = apkUrl,
                ReleaseUrl = releaseUrl,
                ReleaseNotes = notes
            };
        }
        catch
        {
            return null;
        }
    }

    public static async Task DownloadAndApplyWindowsUpdateAsync(string downloadUrl, Action<int>? progressCallback = null)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"BdoCronCalculator_vUpdate_{Guid.NewGuid():N}.exe");
        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;

        if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
        {
            throw new InvalidOperationException("Could not determine current executable path.");
        }

        using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (totalBytes > 0 && progressCallback != null)
                {
                    int percentage = (int)((totalRead * 100) / totalBytes);
                    progressCallback(percentage);
                }
            }
        }

        string updaterScript = Path.Combine(Path.GetTempPath(), $"bdo_update_{Guid.NewGuid():N}.bat");
        string scriptContent = $@"@echo off
chcp 65001 >nul
timeout /t 1 /nobreak >nul
:retry
copy /y ""{tempFile}"" ""{currentExePath}"" >nul 2>&1
if errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto retry
)
del ""{tempFile}"" >nul 2>&1
start """" ""{currentExePath}""
del ""%~f0"" >nul 2>&1
";

        await File.WriteAllTextAsync(updaterScript, scriptContent);

        var psi = new ProcessStartInfo
        {
            FileName = updaterScript,
            CreateNoWindow = true,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(psi);
        Environment.Exit(0);
    }
}
