using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace SkyGlance.Services;

/// <summary>
/// Checks GitHub Releases for a newer tagged version (vMAJOR.MINOR.PATCH) and installs it in place:
/// download next to the running exe, verify the SHA-256 GitHub publishes for the asset,
/// rename the running exe to .old (Windows allows that), move the new one in, restart.
/// </summary>
public static class Updater
{
    public const string Repo = "medovanx/sky-glance";
    public const string AssetName = "SkyGlance.exe";
    public static string ReleasesPage => $"https://github.com/{Repo}/releases";
    public static string RepoPage => $"https://github.com/{Repo}";

    public sealed record Release(Version Version, string Tag, string PageUrl, string AssetUrl, string? Sha256);

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"SkyGlance/{CurrentText}");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    public static Version Current => Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0));
    public static string CurrentText => Current.ToString(3);

    static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

    public static async Task<Release?> CheckAsync()
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest"));
        var r = doc.RootElement;
        string tag = r.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var v)) return null;
        v = Normalize(v);
        if (v <= Current) return null;

        foreach (var a in r.GetProperty("assets").EnumerateArray())
        {
            if (!string.Equals(a.GetProperty("name").GetString(), AssetName, StringComparison.OrdinalIgnoreCase)) continue;
            string? sha = a.TryGetProperty("digest", out var d) && d.GetString() is { } ds && ds.StartsWith("sha256:")
                ? ds["sha256:".Length..] : null;
            return new Release(v, tag, r.GetProperty("html_url").GetString() ?? ReleasesPage,
                               a.GetProperty("browser_download_url").GetString()!, sha);
        }
        return null;
    }

    /// <summary>Where an install is: bytes downloaded and the total (0 when unknown), and whether it is verifying now.</summary>
    public readonly record struct Progress(long Done, long Total, bool Verifying);

    /// <summary>Downloads and swaps the exe, then starts the new version. Returns false if it couldn't.</summary>
    public static async Task<bool> InstallAsync(Release rel, IProgress<Progress>? progress = null)
    {
        string exe = Environment.ProcessPath!;
        string fresh = exe + ".new", old = exe + ".old";
        try
        {
            using (var resp = await Http.GetAsync(rel.AssetUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? 0, done = 0;
                await using var fs = File.Create(fresh);
                await using var src = await resp.Content.ReadAsStreamAsync();
                var buf = new byte[81920];
                var lastReport = DateTime.MinValue;
                int n;
                progress?.Report(new(0, total, false));
                while ((n = await src.ReadAsync(buf)) > 0)
                {
                    await fs.WriteAsync(buf.AsMemory(0, n));
                    done += n;
                    if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 100) { lastReport = DateTime.UtcNow; progress?.Report(new(done, total, false)); }
                }
                progress?.Report(new(done, total, false));
            }

            if (rel.Sha256 != null)
            {
                progress?.Report(new(0, 0, true));
                await using var fs = File.OpenRead(fresh);
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(fs)).ToLowerInvariant();
                if (actual != rel.Sha256.ToLowerInvariant()) { File.Delete(fresh); return false; }
            }

            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);
            try { File.Move(fresh, exe); }
            catch { File.Move(old, exe); throw; }

            Process.Start(new ProcessStartInfo(exe, "--updated") { UseShellExecute = false });
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            try { if (File.Exists(fresh)) File.Delete(fresh); } catch { }
            return false;
        }
    }

    /// <summary>Removes the previous exe left behind by an update.</summary>
    public static void CleanupOld()
    {
        try
        {
            var old = Environment.ProcessPath + ".old";
            if (File.Exists(old)) File.Delete(old);
        }
        catch { /* still locked: try next start */ }
    }

    public static void OpenPage(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
