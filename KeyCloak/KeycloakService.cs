using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnvInit.Models;
using EnvInit.KeyCloak.DTOs;
using EnvInit.KeyCloak.Models;

namespace EnvInit.KeyCloak;

public class KeycloakService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public KeycloakService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAdminTokenAsync(string baseUrl, string username, string password)
    {
        var url = $"{baseUrl}/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

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

    public async Task CreateRealmAsync(string baseUrl, string realmName)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms";
        var data = new RealmRepresentation { Realm = realmName, Enabled = true };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsRealmExistsAsync(string baseUrl, string realmName)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}";
        var response = await _httpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task CreateClientAsync(string baseUrl, string realmName, string clientId, ClientOptions options)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/clients";
        var data = new ClientRepresentation
        {
            ClientId = clientId,
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

    public async Task<bool> IsClientExistsAsync(string baseUrl, string realmName, string clientId)
    {
        AddAuthHeader();
        // Keycloak uses internal ID for certain operations, but we search by clientId here
        var url = $"{baseUrl}/admin/realms/{realmName}/clients?clientId={clientId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        return clients != null && clients.Any();
    }

    public async Task<ClientRepresentation> GetClientByClientIdAsync(string baseUrl, string realmName, string clientId)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/clients?clientId={clientId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        return clients?.FirstOrDefault() ?? throw new Exception($"Client '{clientId}' not found");
    }

    public async Task<string> CreateUserAsync(string baseUrl, string realmName, UserConfig user)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/users";
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

    public async Task<bool> IsUserExistsAsync(string baseUrl, string realmName, string username)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/users?username={username}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<UserRepresentation>>();
        return users != null && users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task CreateRoleAsync(string baseUrl, string realmName, string roleName)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/roles";
        var data = new RoleRepresentation { Name = roleName };
        var response = await _httpClient.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsRoleExistsAsync(string baseUrl, string realmName, string roleName)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/roles/{roleName}";
        var response = await _httpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task AssignRoleAsync(string baseUrl, string realmName, string userId, string roleName)
    {
        AddAuthHeader();
        var roleUrl = $"{baseUrl}/admin/realms/{realmName}/roles/{roleName}";
        var roleResponse = await _httpClient.GetAsync(roleUrl);
        roleResponse.EnsureSuccessStatusCode();
        var role = await roleResponse.Content.ReadFromJsonAsync<RoleRepresentation>();

        var url = $"{baseUrl}/admin/realms/{realmName}/users/{userId}/role-mappings/realm";
        var response = await _httpClient.PostAsJsonAsync(url, new[] { role });
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmManagementRolesToServiceAccountAsync(string baseUrl, string realmName, string clientId)
    {
        var client = await GetClientByClientIdAsync(baseUrl, realmName, clientId);
        var clientInternalId = client.Id;

        AddAuthHeader();
        var saUrl = $"{baseUrl}/admin/realms/{realmName}/clients/{clientInternalId}/service-account-user";
        var saResponse = await _httpClient.GetAsync(saUrl);
        saResponse.EnsureSuccessStatusCode();
        var saUser = await saResponse.Content.ReadFromJsonAsync<UserRepresentation>();
        var saUserId = saUser?.Id ?? throw new Exception("Service account user not found");

        var realmMgmtUrl = $"{baseUrl}/admin/realms/{realmName}/clients?clientId=realm-management";
        var realmMgmtResponse = await _httpClient.GetAsync(realmMgmtUrl);
        realmMgmtResponse.EnsureSuccessStatusCode();
        var realmMgmtClients = await realmMgmtResponse.Content.ReadFromJsonAsync<List<ClientRepresentation>>();
        var realmMgmtClient = realmMgmtClients?.FirstOrDefault() ?? throw new Exception("realm-management client not found");
        var realmMgmtInternalId = realmMgmtClient.Id;

        var rolesUrl = $"{baseUrl}/admin/realms/{realmName}/clients/{realmMgmtInternalId}/roles";
        var rolesResponse = await _httpClient.GetAsync(rolesUrl);
        rolesResponse.EnsureSuccessStatusCode();
        var allRoles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleRepresentation>>();

        var rolesToAssign = new[] { "view-users", "manage-users", "query-users" };
        var roles = allRoles?.Where(r => rolesToAssign.Contains(r.Name)).ToList();

        var assignUrl = $"{baseUrl}/admin/realms/{realmName}/users/{saUserId}/role-mappings/clients/{realmMgmtInternalId}";
        var response = await _httpClient.PostAsJsonAsync(assignUrl, roles);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRealmSettingsAsync(string baseUrl, string realmName, Dictionary<string, object> settings)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}";
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

    public async Task UpdateRealmAttributeSettingsAsync(string baseUrl, string realmName, Dictionary<string, string> attributes)
    {
        await UpdateRealmSettingsAsync(baseUrl, realmName, new Dictionary<string, object> { ["attributes"] = attributes });
    }

    public async Task UpdateClientSettingsAttributesAsync(string baseUrl, string realmName, string clientId, Dictionary<string, string> attributes)
    {
        var client = await GetClientByClientIdAsync(baseUrl, realmName, clientId);
        var clientInternalId = client.Id;

        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/clients/{clientInternalId}";
        var currentResponse = await _httpClient.GetAsync(url);
        currentResponse.EnsureSuccessStatusCode();
        var currentClient = await currentResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        currentClient!["attributes"] = attributes;

        var response = await _httpClient.PutAsJsonAsync(url, currentClient);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetClientSecretAsync(string baseUrl, string realmName, string clientId)
    {
        var client = await GetClientByClientIdAsync(baseUrl, realmName, clientId);
        var clientInternalId = client.Id;

        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/clients/{clientInternalId}/client-secret";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var secret = await response.Content.ReadFromJsonAsync<SecretResponse>();
        return secret?.Value ?? throw new Exception("Failed to get client secret");
    }

    public async Task<string> GetSigningKeyAsync(string baseUrl, string realmName)
    {
        AddAuthHeader();
        var url = $"{baseUrl}/admin/realms/{realmName}/keys";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var keys = await response.Content.ReadFromJsonAsync<KeysResponse>();
        var signingKey = keys?.Keys.FirstOrDefault(k => k.Use == "SIG" && k.Type == "RSA");
        return signingKey?.PublicKey ?? throw new Exception("Signing key not found");
    }
}
