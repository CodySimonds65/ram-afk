namespace RamAfk;

public sealed record AccountIdleInfo(string AccountId, string Label, DateTime LastHostActivityUtc, bool Enabled, DateTime? LastKeepAliveUtc = null, string? LastResult = null)
{
    public TimeSpan IdleFor(DateTime utcNow) => utcNow - LastHostActivityUtc;
}

public sealed record KeepAliveSettings(TimeSpan Threshold, TimeSpan MaxEarlyJitter, TimeSpan MinimumSpacing)
{
    public static KeepAliveSettings Default { get; } = new(TimeSpan.FromMinutes(17), TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(3));
}

public sealed record KeepAliveDue(string AccountId, DateTime DueUtc, TimeSpan EffectiveIdle);
