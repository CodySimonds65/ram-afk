namespace RamAfk;

public sealed class DueSetCalculator
{
    private readonly KeepAliveSettings _settings;
    private readonly Random _random;
    public DueSetCalculator(KeepAliveSettings? settings = null, Random? random = null) { _settings = settings ?? KeepAliveSettings.Default; _random = random ?? Random.Shared; }
    public KeepAliveDue? Calculate(AccountIdleInfo account, DateTime utcNow)
    {
        if (!account.Enabled) return null;
        var jitter = TimeSpan.FromMilliseconds(_random.NextDouble() * _settings.MaxEarlyJitter.TotalMilliseconds);
        var due = account.LastHostActivityUtc + _settings.Threshold - jitter;
        return new KeepAliveDue(account.AccountId, due, account.IdleFor(utcNow));
    }
    public IReadOnlyList<KeepAliveDue> OrderDue(IEnumerable<AccountIdleInfo> accounts, DateTime utcNow)
    {
        return accounts.Select(account => Calculate(account, utcNow)).Where(item => item is not null).Select(item => item!).Where(item => item.DueUtc <= utcNow).OrderBy(item => item.DueUtc).ToArray();
    }
}
