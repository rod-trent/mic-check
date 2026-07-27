namespace MicCheckBot.Options;

/// <summary>
/// Configuration for the calling bot. Bound from the "Bot" section of appsettings /
/// environment. Secrets (AppSecret, certificate) should come from environment variables
/// or a secret store in production, never committed.
/// </summary>
public sealed class BotOptions
{
    /// <summary>Azure AD application (client) id of the bot.</summary>
    public string AppId { get; set; } = "";

    /// <summary>Azure AD client secret. Prefer an env var / Key Vault in production.</summary>
    public string AppSecret { get; set; } = "";

    /// <summary>Azure AD tenant id (or "common" for multi-tenant).</summary>
    public string TenantId { get; set; } = "";

    /// <summary>Public HTTPS base URL of this signaling service (the /api/calling notification URL).</summary>
    public string BotBaseUrl { get; set; } = "";

    /// <summary>Public DNS name of the media endpoint (media-platform module).</summary>
    public string MediaDnsName { get; set; } = "";

    /// <summary>TCP port the media platform listens on (must be publicly reachable).</summary>
    public int MediaPort { get; set; } = 8445;

    /// <summary>Thumbprint of the TLS certificate used by the media platform.</summary>
    public string CertificateThumbprint { get; set; } = "";

    /// <summary>Emit synthetic audio for local testing when true.</summary>
    public bool EnableMockAudio { get; set; } = true;
}
