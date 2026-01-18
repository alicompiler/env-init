using System.Runtime.InteropServices;

namespace EnvInit.Domains;

public class DomainInitializer()
{
    public Task Initialize(List<string> domains)
    {
        var unixHostFilePath = "/etc/hosts";
        var windowsHostFilePath = "C:\\Windows\\System32\\drivers\\etc\\hosts";

        var hostFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? unixHostFilePath : windowsHostFilePath;

        var hostFileContent = File.ReadAllLines(hostFilePath);

        Console.WriteLine("Setting up domain mappings in hosts file.");
        Console.WriteLine("Updating hosts file");
        foreach (var domain in domains)
        {
            var line = $"127.0.0.1 {domain}";
            if (!hostFileContent.Any(x => x.Contains(line)))
            {
                Console.WriteLine("Adding domain mapping to hosts file: " + line);
                File.AppendAllText(hostFilePath, $"\n{line}");
            }
            else
            {
                Console.WriteLine("Domain mapping already exists in hosts file: " + line);
            }
        }

        return Task.CompletedTask;
    }
}
