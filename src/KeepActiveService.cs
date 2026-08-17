namespace RamAfk;

public interface IBackgroundKeepAliveSender
{
    Task<KeepAliveSendResult> SendSpaceAsync(string accountId, CancellationToken cancellationToken);
}

public sealed record KeepAliveSendResult(bool Accepted, string Code, string Message);

public sealed class KeepActiveService(IBackgroundKeepAliveSender sender, KeepAliveSettings? settings = null)
{
    private readonly Dictionary<string, DateTime> _lastSuccess = new(StringComparer.Ordinal);
    private readonly KeepAliveSettings _settings = settings ?? KeepAliveSettings.Default;
    private DateTime _lastDispatchUtc = DateTime.MinValue;

    public IReadOnlyDictionary<string, DateTime> LastSuccess => _lastSuccess;

    public async Task<KeepAliveSendResult> TryKeepAliveAsync(AccountIdleInfo account, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!account.Enabled) return new(false, "disabled", "The account is disabled.");
        if (utcNow - _lastDispatchUtc < _settings.MinimumSpacing) return new(false, "spaced", "Another account was kept alive recently.");
        if (_lastSuccess.TryGetValue(account.AccountId, out var last) && utcNow - last < _settings.Threshold - TimeSpan.FromMinutes(1))
            return new(false, "already-kept-alive", "This account was kept alive recently.");
        var result = await sender.SendSpaceAsync(account.AccountId, cancellationToken);
        if (result.Accepted)
        {
            _lastSuccess[account.AccountId] = utcNow;
            _lastDispatchUtc = utcNow;
        }
        return result;
    }
}
