# RAM AFK

RAM AFK is a standalone Apache-2.0 plugin that keeps user-enabled managed Roblox clients alive with low-priority foreground Space input. It schedules a 17-minute threshold with bounded early jitter, staggers simultaneous due accounts, postpones when a higher-priority action owns an account, retries one failed delivery after 30 seconds, and reports an unprotected account without attempting captcha interaction.

Each keep-alive briefly foregrounds the validated client through the host coordinator and restores the prior foreground client when safe. Focus may switch during delivery; user takeover cancels rather than fighting for focus. Legacy background-message consent is rejected with `foreground-required`.

Build with the .NET 8 Windows SDK. The release package contains `plugin.json`, `ram-afk.exe`, `plugin.zip`, `plugin.sha256`, and a pinned Ed25519 signature.

## Official releases

After a PR is merged, the repository workflow publishes the matching semantic version automatically. If both manifests still contain the latest published version, the workflow creates a patch-only release commit and publishes the next patch version; major and minor version changes remain explicit. Configure `RAM_PLUGIN_SIGNING_KEY` (Ed25519 private PEM) and `RAM_PLUGIN_SIGNING_PUBLIC_KEY` (matching public PEM) repository secrets first. The public key must match the launcher trust anchor; missing secrets fail closed and never publish unsigned official assets. Manual dispatch remains available as a recovery path.
