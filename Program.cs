using System.Text.Json;
using EnvInit.Models;
using EnvInit.Services;
using EnvInit.KeyCloak;
using EnvInit.KeyCloak.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EnvInit;

class Program
{
    static async Task Main(string[] args)
    {
        var configName = "dragon";
        var envFilePath = $"./config/{configName}/env.json";
        var keycloakFilePath = $"./config/{configName}/keycloak.json";

        if (!File.Exists(keycloakFilePath))
        {
            Console.WriteLine($"Keycloak config file not found: {keycloakFilePath}");
            return;
        }

        if (!File.Exists(envFilePath))
        {
            Console.WriteLine($"Env config file not found: {envFilePath}");
            return;
        }

        var keycloakSettingsJson = File.ReadAllText(keycloakFilePath);
        var envSettingsJson = File.ReadAllText(envFilePath);

        var keycloakSettings = JsonSerializer.Deserialize<KeycloakConfig>(keycloakSettingsJson);
        var envSettings = JsonSerializer.Deserialize<EnvConfig>(envSettingsJson);

        if (keycloakSettings == null || envSettings == null)
        {
            Console.WriteLine("Failed to deserialize configuration files.");
            return;
        }

        // Setup DI
        var serviceProvider = new ServiceCollection()
            .AddHttpClient()
            .AddTransient<EnvService>()
            .AddTransient<KeycloakService>()
            .AddTransient<SetupService>()
            .BuildServiceProvider();

        var setupService = serviceProvider.GetRequiredService<SetupService>();

        Console.WriteLine("Starting setup script...");

        try
        {
            // Wait for Keycloak to be ready
            Console.WriteLine($"Waiting for Keycloak at {keycloakSettings.BaseUrl}...");
            if (!await HttpUtils.WaitForKeycloakReadyAsync(keycloakSettings.BaseUrl))
            {
                Console.WriteLine("Keycloak is not ready. Exiting.");
                return;
            }

            await setupService.SetupKeycloakAsync(keycloakSettings);
            await setupService.SetupEnvAsync(keycloakSettings, envSettings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
