using System.Text.Json;
using EnvInit.KeyCloak;

namespace EnvInit.EnvironmentVariables;

public class HamamEnvironmentVariableGenerator(string filePath, KeyCloakService keycloakService) : EnvironmentVariableGenerator
{
    public async Task Setup()
    {
        var clientSecret = await keycloakService.GetClientSecretAsync();
        var signingKey = await keycloakService.GetSigningKeyAsync();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Appsettings file not found at {filePath}. Creating a new one.");
            File.WriteAllText(filePath, "{}");
        }

        var appSettingsContent = File.ReadAllText(filePath);
        Dictionary<string, object> appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(appSettingsContent) ?? new Dictionary<string, object>();
        appSettings["KEY_CLOAK_APPLICATION_CLIENT_SECRET"] = clientSecret;
        appSettings["JWT_SIGNING_KEY"] = signingKey;

        var updatedContent = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, updatedContent);
    }
}
