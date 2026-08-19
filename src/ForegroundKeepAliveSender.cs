namespace RamAfk;

/// <summary>AFK delivery through the host's guarded foreground session.</summary>
public sealed class ForegroundKeepAliveSender(PluginClient client) : IBackgroundKeepAliveSender
{
    public async Task<KeepAliveSendResult> SendSpaceAsync(string accountId, CancellationToken cancellationToken)
    {
        string? sessionId = null;
        try
        {
            var opened = await client.RequestAsync("input.session.open", new
            {
                accountIds = new[] { accountId },
                purpose = "afk",
                restoreForeground = true
            }, cancellationToken).ConfigureAwait(false);
            if (!TrySession(opened, out sessionId, out var openError)) return openError;

            var activated = await client.RequestAsync("input.session.activate", new { sessionId, accountId }, cancellationToken).ConfigureAwait(false);
            if (!IsAccepted(activated, out var activationError)) return activationError;

            var input = new[]
            {
                new
                {
                    kind = "KeyDown", virtualKey = 0x20, scanCode = 0x39, extended = false,
                    button = 0, wheelDelta = 0, normalizedX = 0d, normalizedY = 0d, offsetMicroseconds = 0L
                },
                new
                {
                    kind = "KeyUp", virtualKey = 0x20, scanCode = 0x39, extended = false,
                    button = 0, wheelDelta = 0, normalizedX = 0d, normalizedY = 0d, offsetMicroseconds = 100_000L
                }
            };
            var result = await client.RequestAsync("input.post", new
            {
                accountId,
                sessionId,
                deliveryIntent = "foreground-real",
                events = input
            }, cancellationToken).ConfigureAwait(false);
            return ParseInputResult(result);
        }
        catch (TimeoutException)
        {
            return new(false, "timeout", "The foreground automation host did not respond in time.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(false, "cancelled", "AFK delivery was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            return new(false, "unavailable", ex.Message);
        }
        finally
        {
            if (sessionId is not null)
            {
                try { await client.RequestAsync("input.session.close", new { sessionId, restoreForeground = true, userInitiated = false }, CancellationToken.None).ConfigureAwait(false); }
                catch { }
            }
        }
    }

    private static bool TrySession(PluginClient.Envelope envelope, out string? sessionId, out KeepAliveSendResult error)
    {
        sessionId = null;
        if (envelope.Type != "input.session.result")
        {
            error = new(false, "rejected", "The launcher rejected the AFK foreground session.");
            return false;
        }
        var payload = envelope.Payload;
        var accepted = payload.TryGetProperty("accepted", out var acceptedElement) && acceptedElement.GetBoolean();
        sessionId = payload.TryGetProperty("sessionId", out var idElement) ? idElement.GetString() : null;
        error = new(accepted && !string.IsNullOrWhiteSpace(sessionId),
            payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "");
        return error.Accepted;
    }

    private static bool IsAccepted(PluginClient.Envelope envelope, out KeepAliveSendResult result)
    {
        result = ParseSessionResult(envelope);
        return result.Accepted;
    }

    private static KeepAliveSendResult ParseSessionResult(PluginClient.Envelope envelope)
    {
        if (envelope.Type != "input.session.result") return new(false, "rejected", "The launcher rejected foreground activation.");
        return new(
            envelope.Payload.TryGetProperty("accepted", out var accepted) && accepted.GetBoolean(),
            envelope.Payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            envelope.Payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "");
    }

    private static KeepAliveSendResult ParseInputResult(PluginClient.Envelope envelope)
    {
        if (envelope.Type != "input.result") return new(false, "rejected", "The launcher rejected AFK input.");
        return new(
            envelope.Payload.TryGetProperty("accepted", out var accepted) && accepted.GetBoolean(),
            envelope.Payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            envelope.Payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "");
    }
}
