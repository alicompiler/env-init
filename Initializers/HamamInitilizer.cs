using EnvInit.Domains;
using EnvInit.EnvironmentVariables;
using EnvInit.KeyCloak;

namespace Initializers;

public class HamamInitializer() : Initializer
{
    public async Task Initialize()
    {
        Console.WriteLine("Starting Hamam Initializer...");

        Console.WriteLine("Please enter the path to your local appsettings.json file (to skip hit Enter):");
        var localAppSettingsPath = Console.ReadLine();

        Console.WriteLine("Starting domain initialization...");
        var domainInitializer = new DomainInitializer();
        var domains = new List<string>
        {
        };

        var config = new KeyCloakConfig
        {
            BaseUrl = "http://localhost:9002",
            RealmName = "hamam",
            ClientId = "api",
            AdminUser = "admin",
            AdminPass = "password"
        };
        var keyCloakService = new KeyCloakService(config);
        await domainInitializer.Initialize(domains);
        await SetupKeycloak(keyCloakService, config);

        if (!string.IsNullOrEmpty(localAppSettingsPath))
        {
            var envVarGenerator = new HamamEnvironmentVariableGenerator(localAppSettingsPath, keyCloakService);
            await envVarGenerator.Setup();
        }
    }

    private async Task SetupKeycloak(KeyCloakService keyCloakService, KeyCloakConfig config)
    {
        Console.WriteLine("Starting Hamam Initializer...");
        Console.WriteLine("Setting up Keycloak...");

        Console.WriteLine($"Checking/Creating realm '{config.RealmName}'...");
        if (!await keyCloakService.IsRealmExistsAsync())
        {
            await keyCloakService.CreateRealmAsync();
            Console.WriteLine($"Realm '{config.RealmName}' created.");
        }
        else
        {
            Console.WriteLine($"Realm '{config.RealmName}' already exists.");
        }

        Console.WriteLine($"Checking/Creating client '{config.ClientId}'...");
        if (!await keyCloakService.IsClientExistsAsync())
        {
            await keyCloakService.CreateClientAsync(new ClientOptions
            {
                IsServiceAccountEnabled = true,
                RedirectUris = [],
                WebOrigins = []
            });
            Console.WriteLine($"Client '{config.ClientId}' created in realm '{config.RealmName}'.");
        }
        else
        {
            Console.WriteLine($"Client '{config.ClientId}' already exists in realm '{config.RealmName}'.");
        }

        Console.WriteLine($"Enabling service account for client '{config.ClientId}'...");
        await keyCloakService.AssignRealmManagementRolesToServiceAccountAsync();
        Console.WriteLine($"Service account enabled and roles assigned for client '{config.ClientId}'.");

        Console.WriteLine($"Checking/Creating roles in realm '{config.RealmName}'...");
        var roles = new List<string> { "admin", "super-admin", "customer" };
        foreach (var roleName in roles)
        {
            if (!await keyCloakService.IsRoleExistsAsync(roleName))
            {
                await keyCloakService.CreateRoleAsync(roleName);
                Console.WriteLine($"Role '{roleName}' created in realm '{config.RealmName}'.");
            }
            else
            {
                Console.WriteLine($"Role '{roleName}' already exists in realm '{config.RealmName}'.");
            }
        }

        Console.WriteLine($"Configuring realm settings '{config.RealmName}'...");

        var accessTokenLifespan = 60 * 60;
        var refreshTokenLifespan = 60 * 60 * 24 * 3;

        Console.WriteLine($" - Setting access token lifespan to 1 hour");
        await keyCloakService.UpdateRealmSettingsAsync(new Dictionary<string, object> { ["accessTokenLifespan"] = accessTokenLifespan });

        Console.WriteLine($" - Setting refresh token lifespan to 3 days");
        await keyCloakService.UpdateRealmSettingsAsync(new Dictionary<string, object>
        {
            ["ssoSessionIdleTimeout"] = refreshTokenLifespan,
            ["ssoSessionMaxLifespan"] = refreshTokenLifespan
        });

        Console.WriteLine($"Configuring client settings for client '{config.ClientId}'...");
        var attributes = new Dictionary<string, object>();
        attributes["access.token.lifespan"] = accessTokenLifespan;
        attributes["refresh.token.lifespan"] = refreshTokenLifespan;

        Console.WriteLine($" - Updating client token lifespans, access: 10 minutes, refresh: 30 minutes");
        await keyCloakService.UpdateClientSettingsAttributesAsync(attributes);

        Console.WriteLine($"Checking/Creating 'deleted' profile attributes...");
        if (!await keyCloakService.IsProfileAttributeExistsAsync("deleted"))
        {
            await keyCloakService.CreateProfileAttribute("deleted", true);
            Console.WriteLine($"Profile attribute 'deleted' created.");
        }
        else
        {
            Console.WriteLine($"Profile attribute 'deleted' already exists.");
        }


        var users = new List<UserConfig>();
        var attr = new Dictionary<string, List<string>>
        {
            ["deleted"] = ["false"]
        };

        users = [
            new UserConfig("admin", new List<string> { "admin" }, attr),
            new UserConfig("super", new List<string> { "super-admin" }, attr),
            new UserConfig("customer", new List<string> { "customer" }, attr),
            new UserConfig("customer1", new List<string> { "customer" }, attr),
            new UserConfig("customer2", new List<string> { "customer" }, attr),
            new UserConfig("ali", new List<string> { "customer" }, attr),
            new UserConfig("yousif", new List<string> { "admin" }, attr),
            new UserConfig("ibrahim", new List<string> { "admin" }, attr),
            new UserConfig("haya", new List<string> { "admin" }, attr),
            new UserConfig("tabarak", new List<string> { "admin" }, attr),
            new UserConfig("mina", new List<string> { "admin" }, attr),
            new UserConfig("ahmad", new List<string> { "customer" }, attr),
            new UserConfig("malak", new List<string> { "admin" }, attr),
        ];
        Console.WriteLine($"Creating users in realm '{config.RealmName}'...");
        foreach (var user in users)
        {
            if (!await keyCloakService.IsUserExistsAsync(user.Username))
            {
                var userId = await keyCloakService.CreateUserAsync(user);
                Console.WriteLine($"User '{user.Username}' created in realm '{config.RealmName}'.");
                foreach (var role in user.Roles)
                {
                    await keyCloakService.AssignRoleAsync(userId, role);
                    Console.WriteLine($" - Assigned role '{role}' to user '{user.Username}'.");
                }
            }
            else
            {
                Console.WriteLine($"User '{user.Username}' already exists in realm '{config.RealmName}'.");
            }
        }

        Console.WriteLine("Keycloak setup completed.");
    }
}
