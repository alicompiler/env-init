using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class ClientOptions
{
    [JsonPropertyName("is_service_account_enabled")]
    public bool IsServiceAccountEnabled { get; set; }

    [JsonPropertyName("redirectUris")]
    public List<string> RedirectUris { get; set; } = new();

    [JsonPropertyName("webOrigins")]
    public List<string> WebOrigins { get; set; } = new();
}
