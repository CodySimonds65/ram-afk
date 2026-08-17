# RAM AFK

RAM AFK is a standalone Apache-2.0 plugin that keeps user-enabled managed Roblox clients alive with low-priority background Space messages. It schedules a 17-minute threshold with bounded early jitter, staggers simultaneous due accounts, postpones when a higher-priority action owns an account, retries one failed delivery after 30 seconds, and reports an unprotected account without attempting captcha interaction.

Only activity timestamps and keep-alive results are modeled. No foreground activation or focus restoration is used.

Build with the .NET 8 Windows SDK. The release package contains `plugin.json`, `ram-afk.exe`, `plugin.zip`, `plugin.sha256`, and a pinned Ed25519 signature.

## Official releases

The repository workflow publishes a release from a matching `vMAJOR.MINOR.PATCH` tag or a manual dispatch. Configure `RAM_PLUGIN_SIGNING_KEY` (Ed25519 private PEM) and `RAM_PLUGIN_SIGNING_PUBLIC_KEY` (matching public PEM) repository secrets first. The public key must match the launcher trust anchor; missing secrets fail closed and never publish unsigned official assets.
