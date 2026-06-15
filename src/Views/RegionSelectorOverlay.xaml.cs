using System.Windows;
using System.Windows.Input;

namespace TranslateTool.Views;

public partial class RegionSelectorOverlay : Window
{
    private System.Windows.Point _startPoint;
    private bool _isDragging;

    /// <summary>
    /// 用户选中的区域（屏幕坐标）
    /// </summary>
    public System.Windows.Rect? SelectedRegion { get; private set; }

    /// <summary>
    /// 是否已完成选择
    /// </summary>
    public bool IsCompleted { get; private set; }

    public RegionSelectorOverlay()
    {
        InitializeComponent();
        // 全屏显示
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void Canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(sender as IInputElement);
        _isDragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        SizeLabel.Visibility = Visibility.Visible;
        
        System.Windows.Controls.Canvas.SetLeft(SelectionRect, _startPoint.X);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(sender as IInputElement);
        var x = Math.Min(_startPoint.X, current.X);
        var y = Math.Min(_startPoint.Y, current.Y);
        var w = Math.Abs(current.X - _startPoint.X);
        var h = Math.Abs(current.Y - _startPoint.Y);

        System.Windows.Controls.Canvas.SetLeft(SelectionRect, x);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;

        // 更新尺寸提示
        System.Windows.Controls.Canvas.SetLeft(SizeLabel, x);
        System.Windows.Controls.Canvas.SetTop(SizeLabel, y - 30);
        SizeText.Text = $"{(int)w} × {(int)h}";
    }

    private void Canvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        var current = e.GetPosition(sender as IInputElement);
        var x = Math.Min(_startPoint.X, current.X);
        var y = Math.Min(_startPoint.Y, current.Y);
        var w = Math.Abs(current.X - _startPoint.X);
        var h = Math.Abs(current.Y - _startPoint.Y);

        // 最小选区检查
        if (w < 10 || h < 10)
        {
            SelectedRegion = null;
        }
        else
        {
            // 转换为屏幕坐标
            SelectedRegion = new System.Windows.Rect(
                x + Left,
                y + Top,
                w,
                h);
        }

        IsCompleted = true;
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            IsCompleted = true;
            Close();
        }
    }
}
