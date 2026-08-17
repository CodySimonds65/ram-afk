namespace RamAfk;

public sealed record KeepAliveStatus(string AccountId, DateTime NextDueUtc, DateTime? LastSuccessUtc, string LastResult, bool Protected);

public sealed class KeepAliveScheduler
{
    private readonly KeepAliveSettings _settings;
    private readonly Random _random;
    private readonly IBackgroundKeepAliveSender _sender;
    private readonly Dictionary<string, Schedule> _schedules = new(StringComparer.Ordinal);
    private DateTime _lastDispatchUtc = DateTime.MinValue;

    public KeepAliveScheduler(IBackgroundKeepAliveSender sender, KeepAliveSettings? settings = null, Random? random = null)
    { _sender = sender; _settings = settings ?? KeepAliveSettings.Default; _random = random ?? Random.Shared; }

    public IReadOnlyList<KeepAliveStatus> Statuses => _schedules.Values.Select(schedule => schedule.ToStatus()).OrderBy(status => status.AccountId, StringComparer.Ordinal).ToArray();

    public void UpdateAccounts(IEnumerable<AccountIdleInfo> accounts, DateTime utcNow)
    {
        var seen = accounts.Where(account => account.Enabled).Select(account => account.AccountId).ToHashSet(StringComparer.Ordinal);
        foreach (var account in accounts.Where(account => account.Enabled))
        {
            if (!_schedules.TryGetValue(account.AccountId, out var schedule) || schedule.LastActivityUtc != account.LastHostActivityUtc)
            {
                var jitter = TimeSpan.FromMilliseconds(_random.NextDouble() * _settings.MaxEarlyJitter.TotalMilliseconds);
                _schedules[account.AccountId] = schedule = new Schedule(account.AccountId, account.LastHostActivityUtc,
                    account.LastHostActivityUtc + _settings.Threshold - jitter);
            }
            schedule.Label = account.Label;
        }
        foreach (var id in _schedules.Keys.Where(id => !seen.Contains(id)).ToArray()) _schedules.Remove(id);
    }

    public async Task TickAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        foreach (var schedule in _schedules.Values.OrderBy(value => value.NextDueUtc).ToArray())
        {
            if (schedule.NextDueUtc > utcNow || utcNow - _lastDispatchUtc < _settings.MinimumSpacing) continue;
            var result = await _sender.SendSpaceAsync(schedule.AccountId, cancellationToken).ConfigureAwait(false);
            schedule.LastResult = result.Code;
            if (result.Accepted)
            {
                schedule.LastSuccessUtc = utcNow; schedule.RetryDueUtc = null; schedule.Protected = true;
                schedule.Retried = false;
                schedule.NextDueUtc = utcNow + _settings.Threshold - TimeSpan.FromMilliseconds(_random.NextDouble() * _settings.MaxEarlyJitter.TotalMilliseconds);
                _lastDispatchUtc = utcNow;
            }
            else if (!schedule.Retried)
            {
                schedule.Retried = true; schedule.RetryDueUtc = utcNow + TimeSpan.FromSeconds(30); schedule.NextDueUtc = schedule.RetryDueUtc.Value;
            }
            else
            {
                schedule.Protected = false; schedule.NextDueUtc = DateTime.MaxValue;
            }
        }
    }

    private sealed class Schedule(string accountId, DateTime lastActivityUtc, DateTime nextDueUtc)
    {
        public string AccountId { get; } = accountId; public string Label { get; set; } = accountId;
        public DateTime LastActivityUtc { get; } = lastActivityUtc; public DateTime NextDueUtc { get; set; } = nextDueUtc;
        public DateTime? LastSuccessUtc { get; set; } public DateTime? RetryDueUtc { get; set; }
        public string LastResult { get; set; } = "waiting"; public bool Protected { get; set; } = true; public bool Retried { get; set; }
        public KeepAliveStatus ToStatus() => new(AccountId, NextDueUtc, LastSuccessUtc, LastResult, Protected);
    }
}
