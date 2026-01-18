using System.Text.Json.Serialization;

namespace EnvInit.Models;

public class EnvConfig
{
    [JsonPropertyName("files")]
    public Dictionary<string, string> Files { get; set; } = new();

    [JsonPropertyName("env")]
    public Dictionary<string, string> Env { get; set; } = new();
}
