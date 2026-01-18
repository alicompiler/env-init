using EnvInit.Domains;
using EnvInit.EnvironmentVariables;
using EnvInit.KeyCloak;

namespace Initializers;

public class HamamInitializer() : Initializer
{
    public async Task Initialize()
    {
        Console.WriteLine("Starting Hamam Initializer...");

        Console.WriteLine("Please enter the path to your local appsettings.json file:");
        var localAppSettingsPath = Console.ReadLine();

        if (string.IsNullOrEmpty(localAppSettingsPath))
        {
            Console.WriteLine("Invalid file path provided. Exiting initialization.");
            return;
        }

        Console.WriteLine("Starting domain initialization...");
        var domainInitializer = new DomainInitializer();
        var domains = new List<string>
        {
            "sso.hamam.local",
            "api.hamam.local",
            "admin.hamam.local",
            "db.hamam.local"
        };
        var config = new KeyCloakConfig
        {
            BaseUrl = "http://sso.hamam.local",
            RealmName = "hamam",
            ClientId = "api",
            AdminUser = "admin",
            AdminPass = "admin"
        };
        var keyCloakService = new KeyCloakService(config);
        await domainInitializer.Initialize(domains);
        await SetupKeycloak(keyCloakService, config);
        var envVarGenerator = new HamamEnvironmentVariableGenerator(localAppSettingsPath, keyCloakService);
        await envVarGenerator.Setup();
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

        var accessTokenLifespan = 60 * 10;
        var refreshTokenLifespan = 60 * 30;

        Console.WriteLine($" - Setting access token lifespan to 10 minutes");
        await keyCloakService.UpdateRealmSettingsAsync(new Dictionary<string, object> { ["accessTokenLifespan"] = accessTokenLifespan });

        Console.WriteLine($" - Setting refresh token lifespan to 30 minutes");
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


        var users = new List<UserConfig>();
        users = [
            new UserConfig("admin", new List<string> { "admin" }),
            new UserConfig("super", new List<string> { "super-admin" }),
            new UserConfig("customer1", new List<string> { "customer" }),
            new UserConfig("customer2", new List<string> { "customer" }),
            new UserConfig("ali", new List<string> { "customer" }),
            new UserConfig("yousif", new List<string> { "admin" }),
            new UserConfig("ibrahim", new List<string> { "admin" }),
            new UserConfig("haya", new List<string> { "admin" }),
            new UserConfig("tabarak", new List<string> { "admin" }),
            new UserConfig("mina", new List<string> { "admin" }),
            new UserConfig("ahmad", new List<string> { "customer" }),
            new UserConfig("malak", new List<string> { "admin" }),
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
