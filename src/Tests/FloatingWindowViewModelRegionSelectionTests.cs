using System.Windows;
using TranslateTool.Services;
using TranslateTool.ViewModels;
using Xunit;

namespace TranslateTool.Tests;

public class FloatingWindowViewModelRegionSelectionTests
{
    [Fact]
    public async Task RegionTranslateCommand_UsesInjectedRegionSelectionService()
    {
        var viewModel = new FloatingWindowViewModel(
            new FakeClipboardService(),
            new FakeFilePickerService(),
            new FakeRegionSelectionService(region: null),
            startClipboardMonitor: false);

        await viewModel.RegionTranslateCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string GetText() => "";

        public void SetText(string text)
        {
        }
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public string? PickFileForTranslation() => null;
    }

    private sealed class FakeRegionSelectionService(Rect? region) : IRegionSelectionService
    {
        public Rect? SelectRegion() => region;
    }
}
