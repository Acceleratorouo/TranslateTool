using System.Text.Json.Serialization;

namespace TranslateTool.Models;

public class AiTranslationSettings
{
    [JsonPropertyName("defaultProviderId")]
    public string? DefaultProviderId { get; set; }

    [JsonPropertyName("defaultModel")]
    public string? DefaultModel { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = "你是一位专业翻译助手。请将用户提供的文本翻译成目标语言，只输出译文，不要解释。";

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 2048;

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 60;

    [JsonPropertyName("streamOutput")]
    public bool StreamOutput { get; set; } = false;
}
