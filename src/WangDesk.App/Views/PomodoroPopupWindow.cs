using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WangDesk.App.Services;

using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace WangDesk.App.Views;

public class PomodoroPopupWindow : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IReminderService _reminderService;
    private readonly Popup _popup;
    private DispatcherTimer? _uiTimer;
    private DispatcherTimer? _closeTimer;
    private Border? _rootBorder;
    private bool _mouseHasEntered;
    private bool _isClosing;

    private TextBlock _timeText = null!;
    private Path _progressPath = null!;
    private Slider _intervalSlider = null!;
    private TextBlock _intervalValueLabel = null!;
    private Button _startButton = null!;
    private Button _stopButton = null!;

    private const double CanvasSize = 170;
    private const double RingRadius = 65;
    private const double RingThickness = 6;

    private static readonly SolidColorBrush TomatoColor = new(System.Windows.Media.Color.FromRgb(239, 89, 80));
    private static readonly SolidColorBrush TomatoDarkColor = new(System.Windows.Media.Color.FromRgb(200, 70, 60));
    private static readonly SolidColorBrush BgColor = new(System.Windows.Media.Color.FromRgb(40, 40, 45));
    private static readonly SolidColorBrush BgLightColor = new(System.Windows.Media.Color.FromRgb(55, 55, 60));

    public PomodoroPopupWindow(ISettingsService settingsService, IReminderService reminderService)
    {
        _settingsService = settingsService;
        _reminderService = reminderService;
        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = true
        };
        
        InitializeComponent();
        LoadSettings();
        UpdateDisplay();
        StartUiTimer();
        StartCloseTimer();
        
        _popup.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _uiTimer?.Stop();
        _closeTimer?.Stop();
    }

    public void ShowNearScreenPoint(System.Drawing.Point screenPoint)
    {
        _mouseHasEntered = false;
        _isClosing = false;
        _popup.PlacementRectangle = new Rect(
            screenPoint.X - 140,
            screenPoint.Y - 10,
            280, 0);
        _popup.IsOpen = true;
        _uiTimer?.Start();
        _closeTimer?.Start();
        UpdateDisplay();
        PlayOpenAnimation();
    }

    private void PlayOpenAnimation()
    {
        if (_rootBorder == null) return;

        _rootBorder.Opacity = 0;
        _rootBorder.RenderTransform = new TranslateTransform(0, 12);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var slideUp = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _rootBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((TranslateTransform)_rootBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    public void Close()
    {
        if (_isClosing || !_popup.IsOpen) return;
        _isClosing = true;
        PlayCloseAnimation();
    }

    private void PlayCloseAnimation()
    {
        if (_rootBorder == null)
        {
            _popup.IsOpen = false;
            return;
        }

        _rootBorder.RenderTransform ??= new TranslateTransform(0, 0);

        var fadeOut = new DoubleAnimation(_rootBorder.Opacity, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        var slideDown = new DoubleAnimation(0, 8, TimeSpan.FromMilliseconds(150))
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
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20),
            BorderBrush = BgLightColor,
            BorderThickness = new Thickness(1),
            Width = 280,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 30,
                ShadowDepth = 5,
                Opacity = 0.6
            }
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var canvas = new Canvas
        {
            Width = CanvasSize,
            Height = CanvasSize,
            Margin = new Thickness(0, 8, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var center = CanvasSize / 2;

        var glowRing = new Ellipse
        {
            Width = (RingRadius + 20) * 2,
            Height = (RingRadius + 20) * 2,
            Fill = new RadialGradientBrush
            {
                Center = new System.Windows.Point(0.5, 0.5),
                GradientStops = new GradientStopCollection
                {
                    new(System.Windows.Media.Color.FromRgb(239, 89, 80), 0.7),
                    new(System.Windows.Media.Color.FromRgb(40, 40, 45), 1.0)
                }
            },
            Opacity = 0.3
        };
        Canvas.SetLeft(glowRing, center - RingRadius - 20);
        Canvas.SetTop(glowRing, center - RingRadius - 20);
        canvas.Children.Add(glowRing);

        var outerRing = new Ellipse
        {
            Width = (RingRadius + 12) * 2,
            Height = (RingRadius + 12) * 2,
            Stroke = BgLightColor,
            StrokeThickness = 1,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(outerRing, center - RingRadius - 12);
        Canvas.SetTop(outerRing, center - RingRadius - 12);
        canvas.Children.Add(outerRing);

        var backgroundRing = new Ellipse
        {
            Width = RingRadius * 2,
            Height = RingRadius * 2,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 65)),
            StrokeThickness = RingThickness,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(backgroundRing, center - RingRadius);
        Canvas.SetTop(backgroundRing, center - RingRadius);
        canvas.Children.Add(backgroundRing);

        AddTicks(canvas);

        _progressPath = new Path
        {
            Stroke = TomatoColor,
            StrokeThickness = RingThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(_progressPath);

        var statusText = new TextBlock
        {
            Foreground = TomatoColor,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            Text = "🍅 番茄钟",
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(statusText, center - 30);
        canvas.Children.Add(statusText);

        _timeText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 36,
            FontWeight = FontWeights.Bold,
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(_timeText, center - 12);
        canvas.Children.Add(_timeText);

        Grid.SetRow(canvas, 0);
        grid.Children.Add(canvas);

        var intervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var intervalLabel = new TextBlock
        {
            Text = "⏱ 时长",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 55,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        _intervalSlider = new Slider
        {
            Width = 100,
            Minimum = 1,
            Maximum = 60,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TomatoColor,
            Background = BgLightColor
        };
        _intervalValueLabel = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            Width = 50,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeights.Medium
        };
        _intervalSlider.ValueChanged += OnIntervalChanged;
        intervalPanel.Children.Add(intervalLabel);
        intervalPanel.Children.Add(_intervalSlider);
        intervalPanel.Children.Add(_intervalValueLabel);
        Grid.SetRow(intervalPanel, 1);
        grid.Children.Add(intervalPanel);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _startButton = CreateStyledButton("▶ 启动", TomatoColor);
        _startButton.Margin = new Thickness(0, 0, 16, 0);
        _startButton.Click += OnStartClick;
        
        _stopButton = CreateStyledButton("⏹ 停止", new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 125)));
        _stopButton.Click += OnStopClick;
        buttonPanel.Children.Add(_startButton);
        buttonPanel.Children.Add(_stopButton);
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        _rootBorder.Child = grid;
        _popup.Child = _rootBorder;
    }

    private static Button CreateStyledButton(string content, SolidColorBrush accentColor)
    {
        var defaultBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 55));
        var hoverBg = accentColor;
        var darkerColor = System.Windows.Media.Color.FromRgb(
            (byte)(accentColor.Color.R * 0.8),
            (byte)(accentColor.Color.G * 0.8),
            (byte)(accentColor.Color.B * 0.8));
        var pressedBg = new SolidColorBrush(darkerColor);

        var button = new Button
        {
            Content = content,
            Width = 100,
            Height = 36,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = defaultBg,
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = accentColor,
            BorderThickness = new Thickness(1)
        };

        button.MouseEnter += (s, e) => 
        {
            if (button.IsEnabled) button.Background = hoverBg;
        };
        button.MouseLeave += (s, e) => 
        {
            button.Background = defaultBg;
        };
        button.PreviewMouseDown += (s, e) => 
        {
            button.Background = pressedBg;
        };
        button.PreviewMouseUp += (s, e) => 
        {
            button.Background = button.IsMouseOver ? hoverBg : defaultBg;
        };

        return button;
    }

    private void AddTicks(Canvas canvas)
    {
        var center = CanvasSize / 2;
        for (var i = 0; i < 60; i++)
        {
            var angle = (i * 6) - 90;
            var length = i % 5 == 0 ? 8 : 3;
            var outerRadius = RingRadius + 8;
            var innerRadius = outerRadius - length;

            var rad = angle * Math.PI / 180;
            var x1 = center + outerRadius * Math.Cos(rad);
            var y1 = center + outerRadius * Math.Sin(rad);
            var x2 = center + innerRadius * Math.Cos(rad);
            var y2 = center + innerRadius * Math.Sin(rad);

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = i % 5 == 0 
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 125))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 75, 80)),
                StrokeThickness = i % 5 == 0 ? 2 : 1
            };
            canvas.Children.Add(line);
        }
    }

    private void StartUiTimer()
    {
        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _uiTimer.Tick += (s, e) => UpdateDisplay();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        _intervalSlider.Value = settings.ReminderIntervalMinutes;
        _intervalValueLabel.Text = $"{settings.ReminderIntervalMinutes} 分钟";
    }

    private void OnIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _intervalValueLabel.Text = $"{(int)_intervalSlider.Value} 分钟";
        if (_reminderService.IsRunning)
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        settings.ReminderIntervalMinutes = (int)_intervalSlider.Value;
        _settingsService.SaveSettings();
        _reminderService.SetInterval(settings.ReminderIntervalMinutes);
        UpdateDisplay();
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        _reminderService.Start();
        UpdateDisplay();
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        _reminderService.Stop();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var remaining = _reminderService.GetRemainingTime();
        _timeText.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        UpdateProgress(remaining);

        var isRunning = _reminderService.IsRunning;
        _startButton.IsEnabled = !isRunning;
        _stopButton.IsEnabled = isRunning;
        _intervalSlider.IsEnabled = !isRunning;
        
        _startButton.Opacity = isRunning ? 0.4 : 1;
        _stopButton.Opacity = isRunning ? 1 : 0.4;
    }

    private void UpdateProgress(TimeSpan remaining)
    {
        var totalSeconds = Math.Max(1, _settingsService.CurrentSettings.ReminderIntervalMinutes * 60);
        var remainingSeconds = Math.Max(0, remaining.TotalSeconds);
        var elapsedSeconds = totalSeconds - remainingSeconds;
        var progress = elapsedSeconds / totalSeconds;
        progress = Math.Max(0, Math.Min(1, progress));

        if (progress <= 0)
        {
            _progressPath.Data = null;
            return;
        }

        var center = CanvasSize / 2;
        var radius = RingRadius;
        var startAngle = -90;
        var endAngle = startAngle + 360 * progress;

        var startPoint = PointOnCircle(center, center, radius, startAngle);
        var endPoint = PointOnCircle(center, center, radius, endAngle);

        var largeArc = progress > 0.5;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(startPoint, false, false);
            ctx.ArcTo(
                endPoint,
                new System.Windows.Size(radius, radius),
                0,
                largeArc,
                SweepDirection.Clockwise,
                true,
                false);
        }
        geometry.Freeze();
        _progressPath.Data = geometry;
    }

    private static System.Windows.Point PointOnCircle(double cx, double cy, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180;
        return new System.Windows.Point(
            cx + radius * Math.Cos(radians),
            cy + radius * Math.Sin(radians));
    }

    public void Dispose()
    {
        _uiTimer?.Stop();
        _closeTimer?.Stop();
        _popup.IsOpen = false;
    }
}
