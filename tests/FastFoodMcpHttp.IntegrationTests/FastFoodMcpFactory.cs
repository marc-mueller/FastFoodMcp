using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FastFoodMcpHttp.IntegrationTests;

public class FastFoodMcpFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Explicitly disable API key auth so tests don't depend on user secrets
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FastFoodMcp:Auth:ApiKey:Enabled"] = "false"
            });
        });
    }
}
