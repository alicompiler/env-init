using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}
