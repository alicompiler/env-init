namespace EnvInit.Services;

public static class HttpUtils
{
    public static async Task<bool> WaitForHttpReadyAsync(
        IEnumerable<string> checkUrls,
        int timeoutSeconds = 30,
        float pollInterval = 1.0f,
        Func<HttpResponseMessage, bool>? isSuccess = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        Exception? lastError = null;
        isSuccess ??= (r) => r.IsSuccessStatusCode;

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            foreach (var url in checkUrls)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (isSuccess(response)) return true;
                }
                catch (Exception e)
                {
                    lastError = e;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(pollInterval));
        }

        if (lastError != null)
        {
            Console.WriteLine($"Service readiness check timed out after {timeoutSeconds}s. Last error: {lastError.Message}");
        }
        else
        {
            Console.WriteLine($"Service readiness check timed out after {timeoutSeconds}s.");
        }
        return false;
    }

    public static async Task<bool> WaitForKeycloakReadyAsync(string baseUrl, int timeoutSeconds = 30, float pollInterval = 1.0f)
    {
        var checkUrls = new[]
        {
            $"{baseUrl}/realms/master/.well-known/openid-configuration",
            $"{baseUrl}/realms/master",
            $"{baseUrl}/"
        };
        return await WaitForHttpReadyAsync(checkUrls, timeoutSeconds, pollInterval);
    }
}
