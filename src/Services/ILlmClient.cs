namespace TranslateTool.Services;

/// <summary>
/// LLM 客户端抽象接口
///
/// 提供的核心能力：
/// - <see cref="ChatCompletionAsync"/>：发起对话补全请求并返回文本结果
/// - <see cref="ListModelsAsync"/>：拉取服务端可用模型列表
/// </summary>
public interface ILlmClient : IDisposable
{
    /// <summary>
    /// 调用对话补全接口，返回模型生成的文本内容。
    /// </summary>
    /// <param name="model">目标模型 Id。</param>
    /// <param name="systemPrompt">系统提示词。</param>
    /// <param name="userPrompt">用户输入。</param>
    /// <param name="temperature">采样温度。</param>
    /// <param name="maxTokens">最大生成 token 数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型生成的文本内容；若响应中未包含 content 字段则返回 null。</returns>
    Task<string?> ChatCompletionAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        CancellationToken ct = default);

    /// <summary>
    /// 拉取服务端可用模型列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型 Id 列表。</returns>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);
}
