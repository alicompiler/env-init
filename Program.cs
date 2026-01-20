using EnvInit.Database;
using Initializers;
using Microsoft.Extensions.Logging;

namespace EnvInit;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Choose option: \n1. Initialize Environment\n2. Run Seed Script");
        var choice = Console.ReadLine();
        if (choice == "2")
        {
            var connectionString = "Host=localhost;Username=postgres;Password=password;Database=dragon_system;Port=9001";
            var scriptRunner = new ScriptRunner(connectionString);
            try
            {
                scriptRunner.RunScript("./Seeds/hamam.sql");
                Console.WriteLine("Script executed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script: {ex.Message}");
            }
            return;
        }

        var hamamInitializer = new HamamInitializer();
        await hamamInitializer.Initialize();
    }
}
