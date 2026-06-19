using TranslateTool.Services;
using TranslateTool.Localization;
using TranslateTool.ViewModels;
using Xunit;

namespace TranslateTool.Tests;

public class FloatingWindowViewModelClipboardTests
{
    [Fact]
    public void PasteTranslateCommand_UsesInjectedClipboardService()
    {
        LocalizationManager.Instance.SwitchLanguage("en-US");
        var clipboard = new FakeClipboardService("");
        var viewModel = new FloatingWindowViewModel(clipboard, startClipboardMonitor: false);

        viewModel.PasteTranslateCommand.Execute(null);

        Assert.Equal(LocalizationManager.Instance["ErrorClipboardEmpty"], viewModel.ResultText);
    }

    private sealed class FakeClipboardService(string text) : IClipboardService
    {
        public string Text { get; private set; } = text;

        public string GetText() => Text;

        public void SetText(string text) => Text = text;
    }
}
