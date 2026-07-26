using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace MacAccents.Services;

/// <summary>
/// Checks the project's GitHub Releases for a newer version. Uses the public
/// API (no authentication); any failure is treated as "no update" so the check
/// can never disrupt the app.
/// </summary>
public sealed class GitHubUpdateChecker : IUpdateChecker
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/brielmayer/mac-accents/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    public async Task<UpdateCheckResult> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await Http.GetAsync(LatestReleaseUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return UpdateCheckResult.Failed;

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            JsonElement root = json.RootElement;

            if (!root.TryGetProperty("tag_name", out JsonElement tag) ||
                !root.TryGetProperty("html_url", out JsonElement url) ||
                !TryParseVersion(tag.GetString(), out Version latest))
                return UpdateCheckResult.Failed;

            return latest > Normalize(currentVersion)
                ? UpdateCheckResult.Available(latest, url.GetString() ?? "")
                : UpdateCheckResult.UpToDate;
        }
        catch
        {
            // Network or parsing failure.
            return UpdateCheckResult.Failed;
        }
    }

    /// <summary>Parses a release tag such as "v1.2.3" (or "1.2.3-beta") into a
    /// normalized version.</summary>
    private static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        string cleaned = tag.TrimStart('v', 'V').Split('-')[0];
        if (!Version.TryParse(cleaned, out Version? parsed))
            return false;

        version = Normalize(parsed);
        return true;
    }

    // Compare on Major.Minor.Build only; the assembly's revision component is
    // irrelevant for release comparisons.
    private static Version Normalize(Version v)
        => new(v.Major, v.Minor, Math.Max(v.Build, 0));

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // GitHub's API requires a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MacAccents-UpdateChecker");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
