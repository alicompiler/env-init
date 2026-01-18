using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class RealmRepresentation
{
    [JsonPropertyName("realm")]
    public string Realm { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    [JsonPropertyName("accessTokenLifespan")]
    public int? AccessTokenLifespan { get; set; }

    [JsonPropertyName("ssoSessionIdleTimeout")]
    public int? SsoSessionIdleTimeout { get; set; }

    [JsonPropertyName("ssoSessionMaxLifespan")]
    public int? SsoSessionMaxLifespan { get; set; }
}
