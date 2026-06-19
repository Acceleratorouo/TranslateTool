using TranslateTool.Services;
using TranslateTool.ViewModels;
using Xunit;

namespace TranslateTool.Tests;

public class FloatingWindowViewModelFilePickerTests
{
    [Fact]
    public async Task FileTranslateCommand_UsesInjectedFilePickerService()
    {
        var viewModel = new FloatingWindowViewModel(
            new FakeClipboardService(),
            new FakeFilePickerService(filePath: null),
            startClipboardMonitor: false);

        await viewModel.FileTranslateCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string GetText() => "";

        public void SetText(string text)
        {
        }
    }

    private sealed class FakeFilePickerService(string? filePath) : IFilePickerService
    {
        public string? PickFileForTranslation() => filePath;
    }
}
