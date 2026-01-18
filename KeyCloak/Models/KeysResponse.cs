using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class KeysResponse
{
    [JsonPropertyName("keys")]
    public List<KeyRepresentation> Keys { get; set; } = new();
}
