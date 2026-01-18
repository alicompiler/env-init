using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class SecretResponse
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
