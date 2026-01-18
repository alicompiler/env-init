using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class UserConfig
{
    public UserConfig(string username, List<string> roles)
    {
        Username = username;
        FirstName = char.ToUpper(username[0]) + username[1..];
        LastName = "User";
        Password = "pass" + username;
        Email = username + "@hamam.com";
        Roles = roles;
    }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>> Attributes { get; set; } = new();
}
