using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class KeyRepresentation
{
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; set; }
}
