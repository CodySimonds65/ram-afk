namespace RamAfk;

public sealed class ManagedAccountRegistry
{
    private readonly object _gate = new();
    private IReadOnlyList<ManagedAccountSnapshot> _accounts = [];

    public event EventHandler<int>? Changed;

    public void Replace(IReadOnlyList<ManagedAccountSnapshot> accounts)
    {
        var filtered = accounts.Where(IsUsable).ToArray();
        int count;
        lock (_gate)
        {
            if (SameSet(_accounts, filtered)) return;
            _accounts = filtered;
            count = _accounts.Count;
        }
        Changed?.Invoke(this, count);
    }

    public void Upsert(ManagedAccountSnapshot account)
    {
        if (!IsUsable(account)) return;
        int count;
        lock (_gate)
        {
            var index = IndexOf(account.AccountId);
            if (index >= 0)
            {
                if (_accounts[index] == account) return;
                var updated = _accounts.ToArray(); updated[index] = account; _accounts = updated;
            }
            else
            {
                var expanded = new ManagedAccountSnapshot[_accounts.Count + 1];
                for (var i = 0; i < _accounts.Count; i++) expanded[i] = _accounts[i];
                expanded[^1] = account; _accounts = expanded;
            }
            count = _accounts.Count;
        }
        Changed?.Invoke(this, count);
    }

    public void Remove(string accountId)
    {
        int count;
        lock (_gate)
        {
            var index = IndexOf(accountId);
            if (index < 0) return;
            _accounts = _accounts.Where((_, i) => i != index).ToArray();
            count = _accounts.Count;
        }
        Changed?.Invoke(this, count);
    }

    public IReadOnlyList<ManagedAccountSnapshot> Snapshot()
    {
        lock (_gate) return _accounts.ToArray();
    }

    private int IndexOf(string accountId)
    {
        for (var i = 0; i < _accounts.Count; i++)
            if (string.Equals(_accounts[i].AccountId, accountId, StringComparison.Ordinal)) return i;
        return -1;
    }

    private static bool SameSet(IReadOnlyList<ManagedAccountSnapshot> left, ManagedAccountSnapshot[] right)
    {
        if (left.Count != right.Length) return false;
        foreach (var account in right)
        {
            var matched = false;
            foreach (var candidate in left)
            {
                if (candidate.AccountId == account.AccountId && candidate == account) { matched = true; break; }
            }
            if (!matched) return false;
        }
        return true;
    }

    private static bool IsUsable(ManagedAccountSnapshot account) => account.IsRunning && !string.IsNullOrEmpty(account.AccountId);
}

public sealed record ManagedAccountSnapshot(
    string AccountId,
    string Label,
    int ProcessId,
    long ProcessStartTimeUtcTicks,
    nint WindowHandle,
    int ClientX,
    int ClientY,
    int ClientWidth,
    int ClientHeight,
    uint Dpi,
    bool IsMinimized,
    DateTime LastActivityUtc,
    bool IsRunning,
    nint RootWindowHandle = 0);
