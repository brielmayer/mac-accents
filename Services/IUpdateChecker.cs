namespace MacAccents.Services;

/// <summary>Outcome of an update check.</summary>
public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Failed,
}

/// <summary>Result of an update check.</summary>
public sealed record UpdateCheckResult(UpdateCheckStatus Status, Version? Version, string? ReleaseUrl)
{
    public static readonly UpdateCheckResult UpToDate = new(UpdateCheckStatus.UpToDate, null, null);
    public static readonly UpdateCheckResult Failed = new(UpdateCheckStatus.Failed, null, null);

    public static UpdateCheckResult Available(Version version, string releaseUrl)
        => new(UpdateCheckStatus.UpdateAvailable, version, releaseUrl);
}

/// <summary>Checks whether a newer release of the application is available.</summary>
public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default);
}
