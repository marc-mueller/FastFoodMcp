using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FastFoodMcpHttp.IntegrationTests;

public class FastFoodMcpAuthFactory : FastFoodMcpFactory
{
    public const string ApiKey = "test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["FastFoodMcp:Auth:ApiKey:Enabled"] = "true",
                ["FastFoodMcp:Auth:ApiKey:Key"] = ApiKey,
                ["FastFoodMcp:Auth:ApiKey:HeaderName"] = "X-API-Key"
            };

            config.AddInMemoryCollection(settings);
        });
    }
}
