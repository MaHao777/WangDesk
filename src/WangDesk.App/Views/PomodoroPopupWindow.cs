using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WangDesk.App.Services;

using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace WangDesk.App.Views;

public class PomodoroPopupWindow : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IReminderService _reminderService;
    private readonly Popup _popup;
    private DispatcherTimer? _uiTimer;

    private TextBlock _timeText = null!;
    private Path _progressPath = null!;
    private Slider _intervalSlider = null!;
    private TextBlock _intervalValueLabel = null!;
    private Button _startButton = null!;
    private Button _stopButton = null!;

    private const double CanvasSize = 160;
    private const double RingRadius = 60;
    private const double RingThickness = 8;

    public PomodoroPopupWindow(ISettingsService settingsService, IReminderService reminderService)
    {
        _settingsService = settingsService;
        _reminderService = reminderService;
        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = false
        };
        
        InitializeComponent();
        LoadSettings();
        UpdateDisplay();
        StartUiTimer();
        
        _popup.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _uiTimer?.Stop();
    }

    public void ShowNearScreenPoint(System.Drawing.Point screenPoint)
    {
        _popup.PlacementRectangle = new Rect(
            screenPoint.X - 130,
            screenPoint.Y - 10,
            260, 0);
        _popup.IsOpen = true;
        _uiTimer?.Start();
        UpdateDisplay();
    }

    public void Close()
    {
        _popup.IsOpen = false;
    }

    private void InitializeComponent()
    {
        var rootBorder = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
            BorderThickness = new Thickness(1),
            Width = 260,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 20,
                ShadowDepth = 4,
                Opacity = 0.5
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
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var center = CanvasSize / 2;

        var outerRing = new Ellipse
        {
            Width = (RingRadius + 16) * 2,
            Height = (RingRadius + 16) * 2,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
            StrokeThickness = 1,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(outerRing, center - RingRadius - 16);
        Canvas.SetTop(outerRing, center - RingRadius - 16);
        canvas.Children.Add(outerRing);

        var backgroundRing = new Ellipse
        {
            Width = RingRadius * 2,
            Height = RingRadius * 2,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50)),
            StrokeThickness = RingThickness,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(backgroundRing, center - RingRadius);
        Canvas.SetTop(backgroundRing, center - RingRadius);
        canvas.Children.Add(backgroundRing);

        AddTicks(canvas);

        _progressPath = new Path
        {
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),
            StrokeThickness = RingThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(_progressPath);

        var statusText = new TextBlock
        {
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 150, 150)),
            FontSize = 11,
            Text = "番茄钟",
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(statusText, center - 32);
        canvas.Children.Add(statusText);

        _timeText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 32,
            FontWeight = FontWeights.Bold,
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(_timeText, center - 16);
        canvas.Children.Add(_timeText);

        Grid.SetRow(canvas, 0);
        grid.Children.Add(canvas);

        var intervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var intervalLabel = new TextBlock
        {
            Text = "时长:",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 40,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        _intervalSlider = new Slider
        {
            Width = 110,
            Minimum = 5,
            Maximum = 60,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };
        _intervalValueLabel = new TextBlock
        {
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 55,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
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
        _startButton = CreateStyledButton("▶ 启动", new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)));
        _startButton.Margin = new Thickness(0, 0, 12, 0);
        _startButton.Click += OnStartClick;
        
        _stopButton = CreateStyledButton("⬛ 终止", new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)));
        _stopButton.Click += OnStopClick;
        buttonPanel.Children.Add(_startButton);
        buttonPanel.Children.Add(_stopButton);
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        rootBorder.Child = grid;
        _popup.Child = rootBorder;
    }

    private static Button CreateStyledButton(string content, SolidColorBrush accentColor)
    {
        var defaultBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
        var hoverBg = accentColor;
        var darkerColor = System.Windows.Media.Color.FromRgb(
            (byte)(accentColor.Color.R * 0.8),
            (byte)(accentColor.Color.G * 0.8),
            (byte)(accentColor.Color.B * 0.8));
        var pressedBg = new SolidColorBrush(darkerColor);

        var button = new Button
        {
            Content = content,
            Width = 90,
            Height = 34,
            FontSize = 13,
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
            var length = i % 5 == 0 ? 6 : 3;
            var outerRadius = RingRadius + 10;
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
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 70, 70)),
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
        
        _startButton.Opacity = isRunning ? 0.5 : 1;
        _stopButton.Opacity = isRunning ? 1 : 0.5;
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
        _popup.IsOpen = false;
    }
}
