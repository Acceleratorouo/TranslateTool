namespace TranslateTool.Services;

/// <summary>
/// 文本转语音服务抽象，便于测试和跨平台替换。
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// 异步朗读指定文本，不阻塞调用线程。
    /// </summary>
    void Speak(string text);
}
