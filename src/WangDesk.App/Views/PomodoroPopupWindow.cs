using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using WangDesk.App.Models;
using WangDesk.App.Services;

using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
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

    public bool IsOpen => _popup.IsOpen && !_isClosing;

    private TextBlock _timeText = null!;
    private TextBlock _statusText = null!;
    private Path _progressPath = null!;
    private Slider _focusIntervalSlider = null!;
    private TextBlock _focusIntervalValueLabel = null!;
    private Slider _breakIntervalSlider = null!;
    private TextBlock _breakIntervalValueLabel = null!;
    private ComboBox _reminderSoundComboBox = null!;
    private Border _toggleButtonBorder = null!;
    private TextBlock _toggleButtonText = null!;
    private bool _isLoadingSettings;

    private const double CanvasSize = 170;
    private const double RingRadius = 65;
    private const double RingThickness = 6;

    private static readonly SolidColorBrush TomatoColor = new(System.Windows.Media.Color.FromRgb(239, 89, 80));
    private static readonly SolidColorBrush TomatoDarkColor = new(System.Windows.Media.Color.FromRgb(200, 70, 60));
    private static readonly SolidColorBrush BgColor = new(System.Windows.Media.Color.FromRgb(40, 40, 45));
    private static readonly SolidColorBrush BgLightColor = new(System.Windows.Media.Color.FromRgb(55, 55, 60));
    private const int VkLButton = 0x01;
    private const int VkRButton = 0x02;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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
        ResetOutsideClickState();
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
        else if (HasOutsideClick() || _mouseHasEntered)
        {
            Close();
        }
    }

    private static bool HasOutsideClick()
    {
        return (GetAsyncKeyState(VkLButton) & 0x0001) != 0 ||
               (GetAsyncKeyState(VkRButton) & 0x0001) != 0;
    }

    private static void ResetOutsideClickState()
    {
        _ = GetAsyncKeyState(VkLButton);
        _ = GetAsyncKeyState(VkRButton);
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

        _statusText = new TextBlock
        {
            Foreground = TomatoColor,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            Text = "🍅 专注中",
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(_statusText, center - 30);
        canvas.Children.Add(_statusText);

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

        var focusIntervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var focusIntervalLabel = new TextBlock
        {
            Text = "专注时长",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 72,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        _focusIntervalSlider = CreateStyledSlider();
        _focusIntervalValueLabel = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            Width = 44,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeights.Medium
        };
        _focusIntervalSlider.ValueChanged += OnFocusIntervalChanged;
        focusIntervalPanel.Children.Add(focusIntervalLabel);
        focusIntervalPanel.Children.Add(_focusIntervalSlider);
        focusIntervalPanel.Children.Add(_focusIntervalValueLabel);
        Grid.SetRow(focusIntervalPanel, 1);
        grid.Children.Add(focusIntervalPanel);

        var breakIntervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var breakIntervalLabel = new TextBlock
        {
            Text = "休息时长",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 72,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        _breakIntervalSlider = CreateStyledSlider();
        _breakIntervalValueLabel = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            Width = 44,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeights.Medium
        };
        _breakIntervalSlider.ValueChanged += OnBreakIntervalChanged;
        breakIntervalPanel.Children.Add(breakIntervalLabel);
        breakIntervalPanel.Children.Add(_breakIntervalSlider);
        breakIntervalPanel.Children.Add(_breakIntervalValueLabel);
        Grid.SetRow(breakIntervalPanel, 2);
        grid.Children.Add(breakIntervalPanel);

        var soundPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var soundLabel = new TextBlock
        {
            Text = "提醒音效",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)),
            Width = 72,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        _reminderSoundComboBox = new ComboBox
        {
            Width = 160,
            Height = 28,
            Foreground = System.Windows.Media.Brushes.White,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 65, 74)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 100)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 2, 6, 2),
            FontSize = 12
        };
        _reminderSoundComboBox.Items.Add(CreateSoundItem("系统提示音", ReminderSoundType.Asterisk));
        _reminderSoundComboBox.Items.Add(CreateSoundItem("提醒音", ReminderSoundType.Exclamation));
        _reminderSoundComboBox.Items.Add(CreateSoundItem("蜂鸣音", ReminderSoundType.Beep));
        _reminderSoundComboBox.Items.Add(CreateSoundItem("警示音", ReminderSoundType.Hand));
        _reminderSoundComboBox.Items.Add(CreateSoundItem("问询音", ReminderSoundType.Question));
        _reminderSoundComboBox.SelectionChanged += OnReminderSoundChanged;
        soundPanel.Children.Add(soundLabel);
        soundPanel.Children.Add(_reminderSoundComboBox);
        Grid.SetRow(soundPanel, 3);
        grid.Children.Add(soundPanel);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // 鍦嗚鑳跺泭鍒囨崲鎸夐挳
        _toggleButtonText = new TextBlock
        {
            Text = "▶ 开始专注",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _toggleButtonBorder = new Border
        {
            Width = 140,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = TomatoColor,
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = _toggleButtonText,
            Effect = new DropShadowEffect
            {
                Color = System.Windows.Media.Color.FromRgb(239, 89, 80),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.35
            }
        };

        _toggleButtonBorder.MouseEnter += (s, e) =>
        {
            var isRunning = _reminderService.IsRunning;
            _toggleButtonBorder.Background = isRunning
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 80, 80))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 110, 100));
        };
        _toggleButtonBorder.MouseLeave += (s, e) => UpdateToggleButtonStyle();
        _toggleButtonBorder.MouseLeftButtonDown += (s, e) =>
        {
            var isRunning = _reminderService.IsRunning;
            _toggleButtonBorder.Background = isRunning
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 55, 55))
                : TomatoDarkColor;
        };
        _toggleButtonBorder.MouseLeftButtonUp += (s, e) =>
        {
            OnToggleClick();
        };

        buttonPanel.Children.Add(_toggleButtonBorder);
        Grid.SetRow(buttonPanel, 4);
        grid.Children.Add(buttonPanel);

        _rootBorder.Child = grid;
        _popup.Child = _rootBorder;
    }

    private Slider CreateStyledSlider()
    {
        var slider = new Slider
        {
            Width = 112,
            Minimum = 1,
            Maximum = 60,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Height = 24
        };

        var templateXaml = @"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='Slider'>
    <Grid>
        <Border Background='#41414A' CornerRadius='3' Height='6' VerticalAlignment='Center'/>
        <Track x:Name='PART_Track' VerticalAlignment='Center'>
            <Track.DecreaseRepeatButton>
                <RepeatButton Command='Slider.DecreaseLarge' IsTabStop='False'>
                    <RepeatButton.Template>
                        <ControlTemplate TargetType='RepeatButton'>
                            <Border Background='#EF5950' CornerRadius='3' Height='6'/>
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.DecreaseRepeatButton>
            <Track.IncreaseRepeatButton>
                <RepeatButton Command='Slider.IncreaseLarge' IsTabStop='False'>
                    <RepeatButton.Template>
                        <ControlTemplate TargetType='RepeatButton'>
                            <Border Background='Transparent' Height='6'/>
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.IncreaseRepeatButton>
            <Track.Thumb>
                <Thumb>
                    <Thumb.Template>
                        <ControlTemplate TargetType='Thumb'>
                            <Ellipse Width='16' Height='16' Fill='#EF5950' Stroke='White' StrokeThickness='2'>
                                <Ellipse.Effect>
                                    <DropShadowEffect Color='#EF5950' BlurRadius='8' ShadowDepth='0' Opacity='0.5'/>
                                </Ellipse.Effect>
                            </Ellipse>
                        </ControlTemplate>
                    </Thumb.Template>
                </Thumb>
            </Track.Thumb>
        </Track>
    </Grid>
</ControlTemplate>";

        slider.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(templateXaml);
        return slider;
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
        _isLoadingSettings = true;
        var settings = _settingsService.CurrentSettings;
        _focusIntervalSlider.Value = settings.ReminderIntervalMinutes;
        _breakIntervalSlider.Value = settings.BreakIntervalMinutes;
        _focusIntervalValueLabel.Text = $"{settings.ReminderIntervalMinutes} 分钟";
        _breakIntervalValueLabel.Text = $"{settings.BreakIntervalMinutes} 分钟";
        SelectReminderSound(settings.ReminderSound);
        _isLoadingSettings = false;
    }

    private void OnFocusIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _focusIntervalValueLabel.Text = $"{(int)_focusIntervalSlider.Value} 分钟";
        if (_reminderService.IsRunning)
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        settings.ReminderIntervalMinutes = (int)_focusIntervalSlider.Value;
        _settingsService.SaveSettings();
        _reminderService.SetInterval(settings.ReminderIntervalMinutes);
        UpdateDisplay();
    }

    private void OnBreakIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _breakIntervalValueLabel.Text = $"{(int)_breakIntervalSlider.Value} 分钟";
        if (_reminderService.IsRunning)
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        settings.BreakIntervalMinutes = (int)_breakIntervalSlider.Value;
        _settingsService.SaveSettings();
        _reminderService.SetBreakInterval(settings.BreakIntervalMinutes);
        UpdateDisplay();
    }

    private static ComboBoxItem CreateSoundItem(string label, ReminderSoundType soundType)
    {
        return new ComboBoxItem
        {
            Content = label,
            Tag = soundType
        };
    }

    private void SelectReminderSound(ReminderSoundType soundType)
    {
        foreach (var item in _reminderSoundComboBox.Items)
        {
            if (item is ComboBoxItem comboItem && comboItem.Tag is ReminderSoundType itemSound && itemSound == soundType)
            {
                _reminderSoundComboBox.SelectedItem = comboItem;
                return;
            }
        }

        _reminderSoundComboBox.SelectedIndex = 0;
    }

    private void OnReminderSoundChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings ||
            _reminderSoundComboBox.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not ReminderSoundType selectedSound)
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        settings.ReminderSound = selectedSound;
        _settingsService.SaveSettings();
        ReminderSoundPlayer.Play(selectedSound);
    }

    private void OnToggleClick()
    {
        if (_reminderService.IsRunning)
        {
            _reminderService.Stop();
        }
        else
        {
            _reminderService.StartFocus();
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var remaining = _reminderService.GetRemainingTime();
        _timeText.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        _statusText.Text = _reminderService.CurrentMode == PomodoroMode.Break ? "🌿 休息中" : "🍅 专注中";
        UpdateProgress(remaining);

        var isRunning = _reminderService.IsRunning;
        _focusIntervalSlider.IsEnabled = !isRunning;
        _breakIntervalSlider.IsEnabled = !isRunning;
        UpdateToggleButtonStyle();
    }

    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    private void UpdateToggleButtonStyle()
    {
        var isRunning = _reminderService.IsRunning;
        if (isRunning)
        {
            _toggleButtonText.Text = "■ 停止";
            _toggleButtonBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 65, 65));
            ((DropShadowEffect)_toggleButtonBorder.Effect).Color = System.Windows.Media.Color.FromRgb(130, 65, 65);
        }
        else
        {
            _toggleButtonText.Text = "▶ 开始专注";
            _toggleButtonBorder.Background = TomatoColor;
            ((DropShadowEffect)_toggleButtonBorder.Effect).Color = System.Windows.Media.Color.FromRgb(239, 89, 80);
        }
    }

    private void UpdateProgress(TimeSpan remaining)
    {
        var totalMinutes = _reminderService.CurrentMode == PomodoroMode.Break
            ? _settingsService.CurrentSettings.BreakIntervalMinutes
            : _settingsService.CurrentSettings.ReminderIntervalMinutes;
        var totalSeconds = Math.Max(1, totalMinutes * 60);
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
