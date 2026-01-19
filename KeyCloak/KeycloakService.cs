using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EnvInit.KeyCloak;

public class KeyCloakService
{
    private readonly HttpClient _httpClient;
    private string? _token;
    private readonly KeyCloakConfig _config;

    public KeyCloakService(KeyCloakConfig config)
    {
        _httpClient = new HttpClient();
        _config = config;
        _token = GetAdminTokenAsync().Result;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var url = $"{_config.BaseUrl}/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", _config.AdminUser),
            new KeyValuePair<string, string>("password", _config.AdminPass)
        ]);

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        _token = result?.AccessToken ?? throw new Exception("Failed to get admin token");
        return _token;
    }

    private void AddAuthHeader()
    {
        if (string.IsNullOrEmpty(_token)) throw new Exception("Admin token not set");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task CreateRealmAsync()
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms";
        var data = new RealmRepresentation { Realm = _config.RealmName, Enabled = true };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsRealmExistsAsync()
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}";
        var response = await _httpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task CreateClientAsync(ClientOptions options)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients";
        var data = new ClientRepresentation
        {
            ClientId = _config.ClientId,
            Enabled = true,
            RedirectUris = options.RedirectUris,
            WebOrigins = options.WebOrigins,
            Protocol = "openid-connect",
            StandardFlowEnabled = true,
            PublicClient = false,
            ServiceAccountsEnabled = options.IsServiceAccountEnabled
        };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsClientExistsAsync()
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients?clientId={_config.ClientId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        return clients != null && clients.Any();
    }

    public async Task<ClientRepresentation> GetClientByClientIdAsync()
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients?clientId={_config.ClientId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        return clients?.FirstOrDefault() ?? throw new Exception($"Client '{_config.ClientId}' not found");
    }

    public async Task<string> CreateUserAsync(UserConfig user)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users";
        var data = new UserRepresentation
        {
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Enabled = true,
            Credentials = new List<CredentialRepresentation>
            {
                new CredentialRepresentation { Value = user.Password }
            },
            Attributes = user.Attributes
        };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location;
        return location?.ToString().Split('/').Last() ?? throw new Exception("Failed to get user ID from location header");
    }

    public async Task<bool> IsUserExistsAsync(string username)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users?username={username}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<UserRepresentation>>();
        return users != null && users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task CreateRoleAsync(string roleName)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/roles";
        var data = new RoleRepresentation { Name = roleName };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsRoleExistsAsync(string roleName)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/roles/{roleName}";
        var response = await _httpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        AddAuthHeader();
        var roleUrl = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/roles/{roleName}";
        var roleResponse = await _httpClient.GetAsync(roleUrl);
        roleResponse.EnsureSuccessStatusCode();
        var role = await roleResponse.Content.ReadFromJsonAsync<RoleRepresentation>();

        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users/{userId}/role-mappings/realm";
        var response = await _httpClient.PostAsJsonAsync(url, new[] { role });
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmManagementRolesToServiceAccountAsync()
    {
        var client = await GetClientByClientIdAsync();
        var clientInternalId = client.Id;

        AddAuthHeader();
        var saUrl = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients/{clientInternalId}/service-account-user";
        var saResponse = await _httpClient.GetAsync(saUrl);
        saResponse.EnsureSuccessStatusCode();
        var saUser = await saResponse.Content.ReadFromJsonAsync<UserRepresentation>();
        var saUserId = saUser?.Id ?? throw new Exception("Service account user not found");

        var realmMgmtUrl = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients?clientId=realm-management";
        var realmMgmtResponse = await _httpClient.GetAsync(realmMgmtUrl);
        realmMgmtResponse.EnsureSuccessStatusCode();
        var realmMgmtClients = await realmMgmtResponse.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        var realmMgmtClient = realmMgmtClients?.FirstOrDefault() ?? throw new Exception("realm-management client not found");
        var realmMgmtInternalId = realmMgmtClient.Id;

        var rolesUrl = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients/{realmMgmtInternalId}/roles";
        var rolesResponse = await _httpClient.GetAsync(rolesUrl);
        rolesResponse.EnsureSuccessStatusCode();
        var allRoles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleRepresentation>>();

        var rolesToAssign = new[] { "view-users", "manage-users", "query-users" };
        var roles = allRoles?.Where(r => rolesToAssign.Contains(r.Name)).ToList();

        var assignUrl = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users/{saUserId}/role-mappings/clients/{realmMgmtInternalId}";
        var response = await _httpClient.PostAsJsonAsync(assignUrl, roles);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRealmSettingsAsync(Dictionary<string, object> settings)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}";
        var currentResponse = await _httpClient.GetAsync(url);
        currentResponse.EnsureSuccessStatusCode();
        var currentRealm = await currentResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        foreach (var kvp in settings)
        {
            currentRealm![kvp.Key] = kvp.Value;
        }

        var response = await _httpClient.PutAsJsonAsync(url, currentRealm);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRealmAttributeSettingsAsync(Dictionary<string, string> attributes)
    {
        await UpdateRealmSettingsAsync(new Dictionary<string, object> { ["attributes"] = attributes });
    }

    public async Task UpdateClientSettingsAttributesAsync(Dictionary<string, object> attributes)
    {
        var client = await GetClientByClientIdAsync();
        var clientInternalId = client.Id;

        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients/{clientInternalId}";
        var currentResponse = await _httpClient.GetAsync(url);
        currentResponse.EnsureSuccessStatusCode();
        var currentClient = await currentResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        currentClient!["attributes"] = attributes;

        var response = await _httpClient.PutAsJsonAsync(url, currentClient);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateProfileAttribute(string attributeName, bool isRequired)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users/profile";

        var currentResponse = await _httpClient.GetAsync(url);
        currentResponse.EnsureSuccessStatusCode();
        var profileConfig = await currentResponse.Content.ReadFromJsonAsync<UserProfileConfig>();

        var attributes = profileConfig!.Attributes;

        var newAttribute = new UserProfileAttribute
        {
            Name = attributeName,
            DisplayName = attributeName,
        };
        if (isRequired)
        {
            newAttribute.Required = new UserProfileAttributeRequired
            {
                Roles = ["user"]
            };
            newAttribute.Permissions = new UserProfileAttributePermissions
            {
                View = ["admin"],
                Edit = ["admin"]
            };
        }

        attributes!.Add(newAttribute);
        profileConfig.Attributes = attributes;

        var putResponse = await _httpClient.PutAsJsonAsync(url, profileConfig);
        Console.WriteLine(await putResponse.Content.ReadAsStringAsync());
        putResponse.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsProfileAttributeExistsAsync(string attributeName)
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/users/profile";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var profileConfig = await response.Content.ReadFromJsonAsync<UserProfileConfig>();
        var attributes = profileConfig?.Attributes;
        return attributes != null && attributes.Any(a => a.Name == attributeName);
    }

    public async Task<string> GetClientSecretAsync()
    {
        var client = await GetClientByClientIdAsync();
        var clientInternalId = client.Id;

        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/clients/{clientInternalId}/client-secret";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var secret = await response.Content.ReadFromJsonAsync<SecretResponse>();
        return secret?.Value ?? throw new Exception("Failed to get client secret");
    }

    public async Task<string> GetSigningKeyAsync()
    {
        AddAuthHeader();
        var url = $"{_config.BaseUrl}/admin/realms/{_config.RealmName}/keys";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var keys = await response.Content.ReadFromJsonAsync<KeysResponse>();
        var signingKey = keys?.Keys.FirstOrDefault(k => k.Use == "SIG" && k.Type == "RSA");
        return signingKey?.PublicKey ?? throw new Exception("Signing key not found");
    }
}
