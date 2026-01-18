using System.Text.Json;
using System.Text.Json.Serialization;
using EnvInit.Models;

namespace EnvInit.Services;

public class EnvService
{
    public bool IsEnvFileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public string? GetJsonValue(string filePath, string key)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return data?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
    }

    public void CreateEmptyEnv(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, "{}");
    }

    public void SetJsonValue(string filePath, string key, string value)
    {
        Dictionary<string, object> data;
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
        else
        {
            data = new();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        data[key] = value;
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));
    }
}
