using System.Text.Json.Serialization;

namespace EnvInit.KeyCloak;

public class UserProfileConfig
{
    [JsonPropertyName("attributes")]
    public List<UserProfileAttribute>? Attributes { get; set; }

    [JsonPropertyName("groups")]
    public List<object>? Groups { get; set; }
}

public class UserProfileAttribute
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("required")]
    public UserProfileAttributeRequired? Required { get; set; }

    [JsonPropertyName("permissions")]
    public UserProfileAttributePermissions? Permissions { get; set; }

    [JsonPropertyName("selector")]
    public UserProfileAttributeSelector? Selector { get; set; }
}

public class UserProfileAttributeRequired
{
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }
}

public class UserProfileAttributePermissions
{
    [JsonPropertyName("view")]
    public List<string>? View { get; set; }

    [JsonPropertyName("edit")]
    public List<string>? Edit { get; set; }
}

public class UserProfileAttributeSelector
{
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }
}
