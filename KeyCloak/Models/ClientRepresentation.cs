using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class ClientRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("redirectUris")]
    public List<string>? RedirectUris { get; set; }

    [JsonPropertyName("webOrigins")]
    public List<string>? WebOrigins { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "openid-connect";

    [JsonPropertyName("standardFlowEnabled")]
    public bool StandardFlowEnabled { get; set; } = true;

    [JsonPropertyName("publicClient")]
    public bool PublicClient { get; set; } = false;

    [JsonPropertyName("serviceAccountsEnabled")]
    public bool ServiceAccountsEnabled { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }
}
