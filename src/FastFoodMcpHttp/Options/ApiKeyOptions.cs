namespace FastFoodMcpHttp.Options;

public sealed class ApiKeyOptions
{
    public bool Enabled { get; set; }
    public string? Key { get; set; }
    public string HeaderName { get; set; } = "X-API-Key";
}
