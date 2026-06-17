using System.Text.Json.Serialization;

namespace TranslateTool.Models;

public class LlmProvider
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("homepageUrl")]
    public string? HomepageUrl { get; set; }

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("apiFormat")]
    public LlmApiFormat ApiFormat { get; set; } = LlmApiFormat.OpenAiCompatible;

    [JsonPropertyName("authHeader")]
    public string AuthHeader { get; set; } = "Authorization";

    [JsonPropertyName("authPrefix")]
    public string AuthPrefix { get; set; } = "Bearer";

    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = new();

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;
}
