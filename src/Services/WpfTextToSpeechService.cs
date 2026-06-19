using System.Speech.Synthesis;

namespace TranslateTool.Services;

/// <summary>
/// 基于 Windows SAPI 的文本转语音实现。
/// </summary>
public sealed class WpfTextToSpeechService : ITextToSpeechService
{
    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 异步朗读，不阻塞 UI 线程
        _ = Task.Run(() =>
        {
            try
            {
                using var synthesizer = new SpeechSynthesizer();
                synthesizer.SetOutputToDefaultAudioDevice();
                synthesizer.Speak(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech failed: {ex.Message}");
            }
        });
    }
}
