using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace CKAN.GUI.Avalonia.Services
{
    public record ReleaseInfo(string Tag, string Name, string Body, Uri HtmlUrl, Uri? DmgUrl);

    public static class UpdateChecker
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(UpdateChecker));

        private const string ReleasesApi =
            "https://api.github.com/repos/wjacobs20301/CKAN-MacOS/releases/latest";

        private static readonly string SkipFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CKAN", "skipped-update.txt");

        private static readonly string LastCheckFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CKAN", "last-update-check.txt");

        private static readonly HttpClient http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"CKAN-MacOS/{Meta.GetVersion(Versioning.VersionFormat.Full)}");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return c;
        }

        public static async Task<ReleaseInfo?> CheckAsync()
        {
            try
            {
                var json = await http.GetStringAsync(ReleasesApi);
                var payload = JsonConvert.DeserializeObject<GithubRelease>(json);
                if (payload?.tag_name is null || payload.html_url is null)
                {
                    return null;
                }

                Uri? dmg = null;
                if (payload.assets is not null)
                {
                    foreach (var asset in payload.assets)
                    {
                        if (asset?.browser_download_url is { } url
                            && url.AbsolutePath.EndsWith(".dmg", StringComparison.OrdinalIgnoreCase))
                        {
                            dmg = url;
                            break;
                        }
                    }
                }
                return new ReleaseInfo(
                    payload.tag_name,
                    payload.name ?? payload.tag_name,
                    payload.body ?? "",
                    payload.html_url,
                    dmg);
            }
            catch (HttpRequestException ex)
            {
                log.Info($"Update check network error: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                log.Info("Update check timed out");
                return null;
            }
            catch (Exception ex)
            {
                log.Warn("Update check failed", ex);
                return null;
            }
        }

        /// <summary>Returns true if the last successful check was less than 24h ago.</summary>
        public static bool CheckedWithinLast24Hours()
        {
            try
            {
                if (!File.Exists(LastCheckFile))
                {
                    return false;
                }
                var raw = File.ReadAllText(LastCheckFile).Trim();
                if (!long.TryParse(raw, out var ticks))
                {
                    return false;
                }
                var last = new DateTime(ticks, DateTimeKind.Utc);
                return DateTime.UtcNow - last < TimeSpan.FromHours(24);
            }
            catch
            {
                return false;
            }
        }

        public static void RecordCheckNow()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastCheckFile)!);
                File.WriteAllText(LastCheckFile, DateTime.UtcNow.Ticks.ToString());
            }
            catch (Exception ex)
            {
                log.Debug("Failed to record last-check timestamp", ex);
            }
        }

        public static bool IsNewerThanCurrent(string tag)
        {
            var current = NormalizeVersion(Meta.GetVersion(Versioning.VersionFormat.Normal));
            var latest  = NormalizeVersion(tag);
            return CompareVersions(latest, current) > 0;
        }

        public static bool IsSkipped(string tag)
        {
            try
            {
                return File.Exists(SkipFile)
                    && File.ReadAllText(SkipFile).Trim() == tag;
            }
            catch
            {
                return false;
            }
        }

        public static void Skip(string tag)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SkipFile)!);
                File.WriteAllText(SkipFile, tag);
            }
            catch
            {
            }
        }

        private static string NormalizeVersion(string v)
            => Regex.Replace(v.Trim(), @"^v", "", RegexOptions.IgnoreCase);

        private static int CompareVersions(string a, string b)
        {
            var aParts = a.Split('.', '-', '+');
            var bParts = b.Split('.', '-', '+');
            int len = Math.Max(aParts.Length, bParts.Length);
            for (int i = 0; i < len; i++)
            {
                var aPart = i < aParts.Length ? aParts[i] : "0";
                var bPart = i < bParts.Length ? bParts[i] : "0";
                if (int.TryParse(aPart, out var ai) && int.TryParse(bPart, out var bi))
                {
                    if (ai != bi)
                    {
                        return ai.CompareTo(bi);
                    }
                }
                else
                {
                    var cmp = string.Compare(aPart, bPart, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
            }
            return 0;
        }

        private class GithubRelease
        {
            public string? tag_name { get; set; }
            public string? name { get; set; }
            public string? body { get; set; }
            public Uri? html_url { get; set; }
            public GithubAsset[]? assets { get; set; }
        }

        private class GithubAsset
        {
            public Uri? browser_download_url { get; set; }
        }
    }
}
