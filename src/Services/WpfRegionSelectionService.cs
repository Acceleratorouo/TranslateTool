using System.Windows;
using TranslateTool.Views;

namespace TranslateTool.Services;

public sealed class WpfRegionSelectionService : IRegionSelectionService
{
    public Rect? SelectRegion()
    {
        var overlay = new RegionSelectorOverlay();
        overlay.ShowDialog();

        return overlay.IsCompleted ? overlay.SelectedRegion : null;
    }
}
