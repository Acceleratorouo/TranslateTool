using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace TranslateTool.Services;

/// <summary>
/// 自动更新服务
/// </summary>
public class UpdateService
{
    private static readonly HttpClient Client = HttpShared.Client;
    private const string UpdateCheckUrl = "https://api.github.com/repos/TranslateTool/TranslateTool/releases/latest";
    private const string CurrentVersion = "1.0.0";

    /// <summary>
    /// 检查更新
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, UpdateCheckUrl);
            request.Headers.Add("User-Agent", "TranslateTool/1.0");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");

            var response = await Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("tag_name", out var tagName) &&
                doc.RootElement.TryGetProperty("html_url", out var htmlUrl) &&
                doc.RootElement.TryGetProperty("body", out var body))
            {
                var latestVersion = tagName.GetString()?.TrimStart('v') ?? "";
                var downloadUrl = htmlUrl.GetString() ?? "";
                var releaseNotes = body.GetString() ?? "";

                if (CompareVersions(latestVersion, CurrentVersion) > 0)
                {
                    return new UpdateInfo
                    {
                        LatestVersion = latestVersion,
                        CurrentVersion = CurrentVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseNotes = releaseNotes
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(int.Parse).ToArray();
        var parts2 = v2.Split('.').Select(int.Parse).ToArray();

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;

            if (p1 > p2) return 1;
            if (p1 < p2) return -1;
        }

        return 0;
    }
}

/// <summary>
/// 更新信息
/// </summary>
public class UpdateInfo
{
    public string LatestVersion { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
}
