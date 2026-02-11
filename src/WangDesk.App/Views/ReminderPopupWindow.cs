using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace WangDesk.App.Views;

/// <summary>
/// 番茄钟到时提醒弹窗
/// </summary>
public class ReminderPopupWindow : IDisposable
{
    private readonly Popup _popup;
    private readonly Action? _onAcknowledge;
    private readonly string _title;
    private readonly string _hint;
    private readonly string _buttonText;
    private DispatcherTimer? _autoCloseTimer;
    private DispatcherTimer? _closeTimer;
    private Border? _rootBorder;
    private bool _isClosing;
    private bool _mouseHasEntered;

    private static readonly SolidColorBrush TomatoColor = new(Color.FromRgb(239, 89, 80));
    private static readonly SolidColorBrush BgColor = new(Color.FromRgb(40, 40, 45));
    private static readonly SolidColorBrush BgLightColor = new(Color.FromRgb(55, 55, 60));

    public bool IsOpen => _popup.IsOpen && !_isClosing;

    public ReminderPopupWindow(
        string title = "时间到啦！",
        string hint = "休息一下吧~",
        string buttonText = "知道了",
        Action? onAcknowledge = null)
    {
        _title = title;
        _hint = hint;
        _buttonText = buttonText;
        _onAcknowledge = onAcknowledge;

        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = true
        };

        InitializeComponent();
        StartCloseTimer();
        StartAutoCloseTimer();

        _popup.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _autoCloseTimer?.Stop();
        _closeTimer?.Stop();
    }

    public void ShowNearScreenPoint(System.Drawing.Point screenPoint)
    {
        _mouseHasEntered = false;
        _isClosing = false;
        _popup.PlacementRectangle = new Rect(
            screenPoint.X - 120,
            screenPoint.Y - 10,
            240, 0);
        _popup.IsOpen = true;
        _closeTimer?.Start();
        _autoCloseTimer?.Start();
        PlayOpenAnimation();
    }

    public void Close()
    {
        if (_isClosing || !_popup.IsOpen) return;
        _isClosing = true;
        PlayCloseAnimation();
    }

    private void PlayOpenAnimation()
    {
        if (_rootBorder == null) return;

        _rootBorder.Opacity = 0;
        _rootBorder.RenderTransform = new TranslateTransform(0, 15);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var slideUp = new DoubleAnimation(15, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _rootBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((TranslateTransform)_rootBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    private void PlayCloseAnimation()
    {
        if (_rootBorder == null)
        {
            _popup.IsOpen = false;
            return;
        }

        _rootBorder.RenderTransform ??= new TranslateTransform(0, 0);

        var fadeOut = new DoubleAnimation(_rootBorder.Opacity, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        var slideDown = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (s, e) =>
        {
            _popup.IsOpen = false;
            _isClosing = false;
        };

        _rootBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        ((TranslateTransform)_rootBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideDown);
    }

    private void StartCloseTimer()
    {
        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _closeTimer.Tick += (s, e) => CheckMousePosition();
    }

    private void StartAutoCloseTimer()
    {
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _autoCloseTimer.Tick += (s, e) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };
    }

    private void CheckMousePosition()
    {
        if (!_popup.IsOpen || _rootBorder == null) return;

        var mousePos = Mouse.GetPosition(_rootBorder);
        var width = _rootBorder.ActualWidth;
        var height = _rootBorder.ActualHeight;

        var isInside = mousePos.X >= -5 && mousePos.X <= width + 5 &&
                       mousePos.Y >= -5 && mousePos.Y <= height + 5;

        if (isInside)
        {
            _mouseHasEntered = true;
            // 鼠标在内部时重置自动关闭
            _autoCloseTimer?.Stop();
        }
        else if (_mouseHasEntered)
        {
            Close();
        }
    }

    private void InitializeComponent()
    {
        _rootBorder = new Border
        {
            Background = BgColor,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(20, 16, 20, 16),
            BorderBrush = TomatoColor,
            BorderThickness = new Thickness(1.5),
            Width = 240,
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(239, 89, 80),
                BlurRadius = 25,
                ShadowDepth = 0,
                Opacity = 0.4
            }
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // 番茄图标
        var iconText = new TextBlock
        {
            Text = "🍅",
            FontSize = 36,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(iconText);

        // 标题
        var titleText = new TextBlock
        {
            Text = _title,
            Foreground = TomatoColor,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(titleText);

        // 提示文字
        var hintText = new TextBlock
        {
            Text = _hint,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 185)),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        stack.Children.Add(hintText);

        // 关闭按钮
        var closeButton = new Button
        {
            Content = _buttonText,
            Width = 90,
            Height = 30,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = TomatoColor,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var defaultBg = TomatoColor;
        var hoverBg = new SolidColorBrush(Color.FromRgb(255, 110, 100));
        var pressedBg = new SolidColorBrush(Color.FromRgb(200, 70, 60));

        closeButton.MouseEnter += (s, e) => closeButton.Background = hoverBg;
        closeButton.MouseLeave += (s, e) => closeButton.Background = defaultBg;
        closeButton.PreviewMouseDown += (s, e) => closeButton.Background = pressedBg;
        closeButton.PreviewMouseUp += (s, e) => closeButton.Background = closeButton.IsMouseOver ? hoverBg : defaultBg;
        closeButton.Click += (s, e) =>
        {
            _onAcknowledge?.Invoke();
            Close();
        };

        stack.Children.Add(closeButton);

        _rootBorder.Child = stack;
        _popup.Child = _rootBorder;
    }

    public void Dispose()
    {
        _autoCloseTimer?.Stop();
        _closeTimer?.Stop();
        _popup.IsOpen = false;
    }
}
