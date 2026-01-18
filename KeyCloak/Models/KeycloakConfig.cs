using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class KeycloakConfig
{
    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("admin_user")]
    public string AdminUser { get; set; } = string.Empty;

    [JsonPropertyName("admin_pass")]
    public string AdminPass { get; set; } = string.Empty;

    [JsonPropertyName("realm_name")]
    public string RealmName { get; set; } = string.Empty;

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_options")]
    public ClientOptions ClientOptions { get; set; } = new();

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("users")]
    public List<UserConfig> Users { get; set; } = new();

    [JsonPropertyName("access_token_lifespan")]
    public int? AccessTokenLifespan { get; set; }

    [JsonPropertyName("refresh_token_lifespan")]
    public int? RefreshTokenLifespan { get; set; }

    [JsonPropertyName("client_access_token_lifespan")]
    public int? ClientAccessTokenLifespan { get; set; }

    [JsonPropertyName("client_refresh_token_lifespan")]
    public int? ClientRefreshTokenLifespan { get; set; }

    [JsonPropertyName("frontend_url")]
    public string? FrontendUrl { get; set; }
}
