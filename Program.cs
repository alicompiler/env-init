using Initializers;
using Microsoft.Extensions.Logging;

namespace EnvInit;

class Program
{
    static async Task Main(string[] args)
    {
        var hamamInitializer = new HamamInitializer();
        await hamamInitializer.Initialize();
    }
}
