using Microsoft.AspNetCore.Mvc.Testing;

namespace FastFoodMcpHttp.IntegrationTests;

public class FastFoodMcpFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        return base.CreateHost(builder);
    }
}
