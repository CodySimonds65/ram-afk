using RamAfk;
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
var settings = new KeepAliveSettings(TimeSpan.FromMinutes(17), TimeSpan.Zero, TimeSpan.FromSeconds(3));
var calculator = new DueSetCalculator(settings, new Random(1));
var now = DateTime.UtcNow;
var due = calculator.Calculate(new AccountIdleInfo("a", "A", now - TimeSpan.FromMinutes(17), true), now);
Require(due is not null && due.DueUtc <= now, "Due calculation failed.");
var sent = 0; var service = new KeepActiveService(new FakeSender(() => { sent++; return new KeepAliveSendResult(true, "ok", "posted"); }), settings);
var result = await service.TryKeepAliveAsync(new AccountIdleInfo("a", "A", now - TimeSpan.FromMinutes(17), true), now, CancellationToken.None);
Require(result.Accepted && sent == 1, "Keep-alive dispatch failed.");
var second = await service.TryKeepAliveAsync(new AccountIdleInfo("a", "A", now - TimeSpan.FromMinutes(17), true), now.AddSeconds(1), CancellationToken.None);
Require(!second.Accepted && second.Code is "spaced" or "already-kept-alive", "Duplicate keep-alive was not blocked.");
Console.WriteLine("RAM AFK tests passed.");

file sealed class FakeSender(Func<KeepAliveSendResult> callback) : IBackgroundKeepAliveSender { public Task<KeepAliveSendResult> SendSpaceAsync(string accountId, CancellationToken cancellationToken) => Task.FromResult(callback()); }
