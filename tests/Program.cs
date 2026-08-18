using RamAfk;
using System.Reflection;
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
var tokenPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".token");
await File.WriteAllTextAsync(tokenPath, "test-token");
var launchClient = PluginClient.FromArgs(["--ram-plugin", "--pipe", "test-pipe", "--token-file", tokenPath, "--plugin-id", "io.github.codysimonds65.ram.afk", "--data", "test-data"]);
Require(launchClient is not null && !File.Exists(tokenPath), "Plugin launch arguments did not preserve the host pipe and token-file values.");
var pluginIdField = typeof(PluginClient).GetField("_pluginId", BindingFlags.Instance | BindingFlags.NonPublic);
var tokenField = typeof(PluginClient).GetField("_token", BindingFlags.Instance | BindingFlags.NonPublic);
Require((string?)pluginIdField?.GetValue(launchClient) == "io.github.codysimonds65.ram.afk", "Plugin launch arguments did not preserve the plugin ID.");
Require((string?)tokenField?.GetValue(launchClient) == "test-token", "Plugin launch arguments did not preserve the token.");
await launchClient!.DisposeAsync();
Require(PluginClient.FromArgs(["--ram-plugin", "--pipe"]) is null, "A missing pipe value was accepted.");
Require(PluginClient.FromArgs(["--ram-plugin", "--pipe", "", "--plugin-id", "io.github.codysimonds65.ram.afk", "--token", "test-token"]) is null, "An empty pipe value was accepted.");
Require(PluginClient.FromArgs(["--ram-plugin", "--pipe", "test-pipe", "--plugin-id"]) is null, "A missing plugin ID value was accepted.");
Require(PluginClient.FromArgs(["--ram-plugin", "--pipe", "test-pipe", "--plugin-id", "io.github.codysimonds65.ram.afk", "--token-file"]) is null, "A missing token-file value was accepted.");
Require(PluginClient.FromArgs(["--ram-plugin", "--pipe", "test-pipe", "--plugin-id", "io.github.codysimonds65.ram.afk", "--token-file", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing")]) is null, "A missing token file was not rejected safely.");
Console.WriteLine("RAM AFK tests passed.");

file sealed class FakeSender(Func<KeepAliveSendResult> callback) : IBackgroundKeepAliveSender { public Task<KeepAliveSendResult> SendSpaceAsync(string accountId, CancellationToken cancellationToken) => Task.FromResult(callback()); }
