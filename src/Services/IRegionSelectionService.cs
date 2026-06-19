using System.Windows;

namespace TranslateTool.Services;

public interface IRegionSelectionService
{
    Rect? SelectRegion();
}
