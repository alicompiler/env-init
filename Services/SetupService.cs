using EnvInit.Models;
using EnvInit.KeyCloak;
using EnvInit.KeyCloak.Models;
using EnvInit.KeyCloak.DTOs;

namespace EnvInit.Services;

public class SetupService
{
    private readonly KeycloakService _keycloakService;
    private readonly EnvService _envService;

    public SetupService(KeycloakService keycloakService, EnvService envService)
    {
        _keycloakService = keycloakService;
        _envService = envService;
    }

    public async Task SetupKeycloakAsync(KeycloakConfig settings)
    {
        var baseUrl = settings.BaseUrl;
        var adminUser = settings.AdminUser;
        var adminPass = settings.AdminPass;
        var realmName = settings.RealmName;
        var clientId = settings.ClientId;
        var clientOptions = settings.ClientOptions;
        var users = settings.Users;
        var roles = settings.Roles;

        Console.WriteLine("Setting up Keycloak...");
        Console.WriteLine($"Connecting to Keycloak at {baseUrl} with admin user '{adminUser}'");
        var token = await _keycloakService.GetAdminTokenAsync(baseUrl, adminUser, adminPass);

        Console.WriteLine($"Checking/Creating realm '{realmName}'...");
        if (!await _keycloakService.IsRealmExistsAsync(baseUrl, realmName))
        {
            await _keycloakService.CreateRealmAsync(baseUrl, realmName);
            Console.WriteLine($"Realm '{realmName}' created.");
        }
        else
        {
            Console.WriteLine($"Realm '{realmName}' already exists.");
        }

        Console.WriteLine($"Checking/Creating client '{clientId}'...");
        if (!await _keycloakService.IsClientExistsAsync(baseUrl, realmName, clientId))
        {
            await _keycloakService.CreateClientAsync(baseUrl, realmName, clientId, clientOptions);
            Console.WriteLine($"Client '{clientId}' created in realm '{realmName}'.");
        }
        else
        {
            Console.WriteLine($"Client '{clientId}' already exists in realm '{realmName}'.");
        }

        if (clientOptions.IsServiceAccountEnabled)
        {
            Console.WriteLine($"Enabling service account for client '{clientId}'...");
            await _keycloakService.AssignRealmManagementRolesToServiceAccountAsync(baseUrl, realmName, clientId);
            Console.WriteLine($"Service account enabled and roles assigned for client '{clientId}'.");
        }

        Console.WriteLine($"Checking/Creating roles and users in realm '{realmName}'...");
        foreach (var roleName in roles)
        {
            if (!await _keycloakService.IsRoleExistsAsync(baseUrl, realmName, roleName))
            {
                await _keycloakService.CreateRoleAsync(baseUrl, realmName, roleName);
                Console.WriteLine($"Role '{roleName}' created in realm '{realmName}'.");
            }
            else
            {
                Console.WriteLine($"Role '{roleName}' already exists in realm '{realmName}'.");
            }
        }

        Console.WriteLine($"Configuring realm settings '{realmName}'...");
        if (settings.AccessTokenLifespan.HasValue)
        {
            Console.WriteLine($" - Setting access token lifespan to {settings.AccessTokenLifespan} seconds");
            await _keycloakService.UpdateRealmSettingsAsync(baseUrl, realmName, new Dictionary<string, object> { ["accessTokenLifespan"] = settings.AccessTokenLifespan.Value });
        }

        if (settings.RefreshTokenLifespan.HasValue)
        {
            Console.WriteLine($" - Setting refresh token lifespan to {settings.RefreshTokenLifespan} seconds");
            await _keycloakService.UpdateRealmSettingsAsync(baseUrl, realmName, new Dictionary<string, object> 
            { 
                ["ssoSessionIdleTimeout"] = settings.RefreshTokenLifespan.Value,
                ["ssoSessionMaxLifespan"] = settings.RefreshTokenLifespan.Value
            });
        }

        if (!string.IsNullOrEmpty(settings.FrontendUrl))
        {
            Console.WriteLine($" - Setting frontend URL to {settings.FrontendUrl}");
            await _keycloakService.UpdateRealmAttributeSettingsAsync(baseUrl, realmName, new Dictionary<string, string> { ["frontendUrl"] = settings.FrontendUrl });
        }

        Console.WriteLine($"Configuring client settings for client '{clientId}'...");
        if (settings.ClientAccessTokenLifespan.HasValue || settings.ClientRefreshTokenLifespan.HasValue)
        {
            var attributes = new Dictionary<string, string>();
            if (settings.ClientAccessTokenLifespan.HasValue) attributes["access.token.lifespan"] = settings.ClientAccessTokenLifespan.Value.ToString();
            if (settings.ClientRefreshTokenLifespan.HasValue) attributes["refresh.token.lifespan"] = settings.ClientRefreshTokenLifespan.Value.ToString();

            Console.WriteLine($" - Updating client token lifespans, access: {settings.ClientAccessTokenLifespan}, refresh: {settings.ClientRefreshTokenLifespan}");
            await _keycloakService.UpdateClientSettingsAttributesAsync(baseUrl, realmName, clientId, attributes);
        }

        if (users.Count > 0)
        {
            Console.WriteLine($"Creating users in realm '{realmName}'...");
        }
        else
        {
            Console.WriteLine($"No users to create in realm '{realmName}'.");
        }

        foreach (var user in users)
        {
            if (!await _keycloakService.IsUserExistsAsync(baseUrl, realmName, user.Username))
            {
                var userId = await _keycloakService.CreateUserAsync(baseUrl, realmName, user);
                Console.WriteLine($"User '{user.Username}' created in realm '{realmName}'.");
                foreach (var role in user.Roles)
                {
                    await _keycloakService.AssignRoleAsync(baseUrl, realmName, userId, role);
                    Console.WriteLine($" - Assigned role '{role}' to user '{user.Username}'.");
                }
            }
            else
            {
                Console.WriteLine($"User '{user.Username}' already exists in realm '{realmName}'.");
            }
        }

        Console.WriteLine("Keycloak setup completed.");
    }

    public async Task SetupEnvAsync(KeycloakConfig keycloakSettings, EnvConfig envSettings)
    {
        var files = envSettings.Files;
        var env = envSettings.Env;
        var baseUrl = keycloakSettings.BaseUrl;
        var realmName = keycloakSettings.RealmName;
        var clientId = keycloakSettings.ClientId;
        var adminUsername = keycloakSettings.AdminUser;
        var adminPassword = keycloakSettings.AdminPass;

        var adminToken = await _keycloakService.GetAdminTokenAsync(baseUrl, adminUsername, adminPassword);

        foreach (var kvp in env)
        {
            var key = kvp.Key;
            var fileNameReference = kvp.Value;
            if (!files.TryGetValue(fileNameReference, out var filePath))
            {
                Console.WriteLine($"File reference '{fileNameReference}' not found in 'files' configuration.");
                continue;
            }

            if (!_envService.IsEnvFileExists(filePath))
            {
                Console.WriteLine($"File '{filePath}' does not exist, will create it.");
                _envService.CreateEmptyEnv(filePath);
            }

            var currentValue = _envService.GetJsonValue(filePath, key);
            if (currentValue != null)
            {
                Console.WriteLine($"Env '{key}' already set to value '{currentValue}' in file '{filePath}', overriding...");
            }

            if (key == "client_secret")
            {
                var clientSecret = await _keycloakService.GetClientSecretAsync(baseUrl, realmName, clientId);
                _envService.SetJsonValue(filePath, key, clientSecret);
                Console.WriteLine($" - Set client secret for client '{clientId}' in realm '{realmName}'");
            }
            else if (key == "signing_key")
            {
                var signingKey = await _keycloakService.GetSigningKeyAsync(baseUrl, realmName);
                _envService.SetJsonValue(filePath, key, signingKey);
                Console.WriteLine($" - Set signing key for realm '{realmName}'");
            }
        }
    }
}
