namespace EnvInit.KeyCloak;

public class KeyCloakConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string AdminPass { get; set; } = string.Empty;
}
