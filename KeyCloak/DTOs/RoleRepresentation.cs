using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class RoleRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
