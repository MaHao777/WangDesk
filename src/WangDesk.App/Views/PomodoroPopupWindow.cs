using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WangDesk.App.Models;
using WangDesk.App.Services;

using Brushes = System.Windows.Media.Brushes;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using IoPath = System.IO.Path;
using Orientation = System.Windows.Controls.Orientation;
using ShapePath = System.Windows.Shapes.Path;
using SystemColors = System.Windows.SystemColors;
using WpfControl = System.Windows.Controls.Control;
using WpfButton = System.Windows.Controls.Button;

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
    private bool _isLoadingSettings;
    private bool _isSettingsView;
    private DateTime _suppressAutoCloseUntil = DateTime.MinValue;

    public bool IsOpen => _popup.IsOpen && !_isClosing;

    private TextBlock _timeText = null!;
    private TextBlock _todayFocusLabelText = null!;
    private TextBlock _todayFocusValueText = null!;
    private TextBlock _todayFocusHintText = null!;
    private ShapePath _progressPath = null!;
    private Slider _focusIntervalSlider = null!;
    private TextBlock _focusIntervalValueLabel = null!;
    private Slider _breakIntervalSlider = null!;
    private TextBlock _breakIntervalValueLabel = null!;
    private ComboBox _reminderSoundComboBox = null!;
    private WpfButton _addCustomSoundButton = null!;
    private WpfButton _removeCustomSoundButton = null!;
    private StackPanel _idleActionButtonsPanel = null!;
    private Border _startFocusButtonBorder = null!;
    private Border _startBreakButtonBorder = null!;
    private Border _stopCurrentButtonBorder = null!;
    private TextBlock _stopCurrentButtonText = null!;
    private Grid _mainPanel = null!;
    private Grid _settingsPanel = null!;
    private Border _openSettingsButton = null!;
    private Border _backButton = null!;
    private Border _todayFocusHeaderBorder = null!;
    private ScaleTransform _todayFocusHeaderScaleTransform = null!;
    private TranslateTransform _todayFocusHeaderTranslateTransform = null!;
    private TextBlock _headerTitle = null!;
    private string? _lastValidSoundSelectionId;

    private const double CanvasSize = 170;
    private const double RingRadius = 65;
    private const double RingThickness = 6;

    private static readonly SolidColorBrush TomatoColor = new(System.Windows.Media.Color.FromRgb(239, 89, 80));
    private static readonly SolidColorBrush TomatoDarkColor = new(System.Windows.Media.Color.FromRgb(200, 70, 60));
    private static readonly SolidColorBrush BreakColor = new(System.Windows.Media.Color.FromRgb(112, 196, 144));
    private static readonly SolidColorBrush BgColor = new(System.Windows.Media.Color.FromRgb(40, 40, 45));
    private static readonly SolidColorBrush BgLightColor = new(System.Windows.Media.Color.FromRgb(55, 55, 60));
    private const int VkLButton = 0x01;
    private const int VkRButton = 0x02;

    private static readonly (string Id, string Label)[] BuiltinSoundOptions =
    [
        (ReminderSoundPlayer.BuiltinAsteriskId, "系统提示音"),
        (ReminderSoundPlayer.BuiltinExclamationId, "提醒音"),
        (ReminderSoundPlayer.BuiltinBeepId, "蜂鸣音"),
        (ReminderSoundPlayer.BuiltinHandId, "警示音"),
        (ReminderSoundPlayer.BuiltinQuestionId, "问询音")
    ];

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
        _isSettingsView = false;
        UpdateViewVisibility();
        LoadSettings();
        ResetOutsideClickState();
        StartAutoCloseGracePeriod();
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
        if (DateTime.Now < _suppressAutoCloseUntil) return;

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

    private void StartAutoCloseGracePeriod()
    {
        _suppressAutoCloseUntil = DateTime.Now.AddMilliseconds(450);
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

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headerGrid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 6)
        };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _backButton = CreateIconButton("←", () =>
        {
            _isSettingsView = false;
            _mouseHasEntered = false;
            ResetOutsideClickState();
            StartAutoCloseGracePeriod();
            UpdateViewVisibility();
        });
        _backButton.Visibility = Visibility.Collapsed;
        Grid.SetColumn(_backButton, 0);
        headerGrid.Children.Add(_backButton);

        _todayFocusValueText = new TextBlock
        {
            Text = "00:00",
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        _todayFocusLabelText = new TextBlock
        {
            Text = "今日已专注",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 182)),
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var todayFocusSummaryRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        todayFocusSummaryRow.Children.Add(_todayFocusLabelText);
        todayFocusSummaryRow.Children.Add(_todayFocusValueText);

        _todayFocusHintText = new TextBlock
        {
            Text = "Patience is key in life",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(168, 169, 176)),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0),
            Opacity = 0,
            MaxHeight = 0,
            Visibility = Visibility.Collapsed
        };

        var todayFocusStatsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        todayFocusStatsPanel.Children.Add(todayFocusSummaryRow);
        todayFocusStatsPanel.Children.Add(_todayFocusHintText);

        _todayFocusHeaderScaleTransform = new ScaleTransform(1, 1);
        _todayFocusHeaderTranslateTransform = new TranslateTransform(0, 0);
        var todayFocusHeaderTransformGroup = new TransformGroup();
        todayFocusHeaderTransformGroup.Children.Add(_todayFocusHeaderScaleTransform);
        todayFocusHeaderTransformGroup.Children.Add(_todayFocusHeaderTranslateTransform);

        _todayFocusHeaderBorder = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 36, 37, 46)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(-20, -20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            RenderTransform = todayFocusHeaderTransformGroup,
            RenderTransformOrigin = new System.Windows.Point(0, 0),
            Visibility = Visibility.Visible,
            Child = todayFocusStatsPanel
        };
        _todayFocusHeaderBorder.MouseEnter += (s, e) => AnimateTodayFocusHeader(hovered: true);
        _todayFocusHeaderBorder.MouseLeave += (s, e) => AnimateTodayFocusHeader(hovered: false);
        Grid.SetColumn(_todayFocusHeaderBorder, 0);
        headerGrid.Children.Add(_todayFocusHeaderBorder);

        _headerTitle = new TextBlock
        {
            Text = "番茄设置",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(190, 190, 195)),
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        Grid.SetColumn(_headerTitle, 1);
        headerGrid.Children.Add(_headerTitle);

        _openSettingsButton = CreateIconButton("⚙", () =>
        {
            _isSettingsView = true;
            _mouseHasEntered = false;
            ResetOutsideClickState();
            StartAutoCloseGracePeriod();
            LoadSettings();
            UpdateViewVisibility();
        });
        _openSettingsButton.Margin = new Thickness(0, -20, -20, 0);
        Grid.SetColumn(_openSettingsButton, 2);
        headerGrid.Children.Add(_openSettingsButton);

        Grid.SetRow(headerGrid, 0);
        rootGrid.Children.Add(headerGrid);

        _mainPanel = BuildMainPanel();
        Grid.SetRow(_mainPanel, 1);
        rootGrid.Children.Add(_mainPanel);

        _settingsPanel = BuildSettingsPanel();
        Grid.SetRow(_settingsPanel, 1);
        rootGrid.Children.Add(_settingsPanel);

        _rootBorder.Child = rootGrid;
        _popup.Child = _rootBorder;
        UpdateViewVisibility();
    }

    private Grid BuildMainPanel()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var canvas = new Canvas
        {
            Width = CanvasSize,
            Height = CanvasSize,
            Margin = new Thickness(0, 4, 0, 16),
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
            Fill = Brushes.Transparent
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
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(backgroundRing, center - RingRadius);
        Canvas.SetTop(backgroundRing, center - RingRadius);
        canvas.Children.Add(backgroundRing);

        AddTicks(canvas);

        _progressPath = new ShapePath
        {
            Stroke = TomatoColor,
            StrokeThickness = RingThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(_progressPath);

        _timeText = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 36,
            FontWeight = FontWeights.Bold,
            Width = CanvasSize,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(_timeText, center - 18);
        canvas.Children.Add(_timeText);

        Grid.SetRow(canvas, 0);
        grid.Children.Add(canvas);

        var actionButtonsHost = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _idleActionButtonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _startFocusButtonBorder = CreateActionButton("开始专注", 104);
        _startFocusButtonBorder.Margin = new Thickness(0, 0, 10, 0);
        ConfigureActionButtonInteractions(
            _startFocusButtonBorder,
            () => GetButtonBaseColor(isRunning: false, isBreakMode: false),
            () => GetButtonHoverColor(isRunning: false, isBreakMode: false),
            () => GetButtonPressedColor(isRunning: false, isBreakMode: false),
            OnStartFocusClick);
        _idleActionButtonsPanel.Children.Add(_startFocusButtonBorder);

        _startBreakButtonBorder = CreateActionButton("开始休息", 104);
        ConfigureActionButtonInteractions(
            _startBreakButtonBorder,
            () => GetButtonBaseColor(isRunning: false, isBreakMode: true),
            () => GetButtonHoverColor(isRunning: false, isBreakMode: true),
            () => GetButtonPressedColor(isRunning: false, isBreakMode: true),
            OnStartBreakClick);
        _idleActionButtonsPanel.Children.Add(_startBreakButtonBorder);

        _stopCurrentButtonText = CreateActionButtonText("结束专注");
        _stopCurrentButtonBorder = CreateActionButtonBorder(_stopCurrentButtonText, 140);
        ConfigureActionButtonInteractions(
            _stopCurrentButtonBorder,
            () => GetButtonBaseColor(isRunning: true, isBreakMode: _reminderService.CurrentMode == PomodoroMode.Break),
            () => GetButtonHoverColor(isRunning: true, isBreakMode: _reminderService.CurrentMode == PomodoroMode.Break),
            () => GetButtonPressedColor(isRunning: true, isBreakMode: _reminderService.CurrentMode == PomodoroMode.Break),
            OnStopCurrentClick);

        actionButtonsHost.Children.Add(_idleActionButtonsPanel);
        actionButtonsHost.Children.Add(_stopCurrentButtonBorder);
        Grid.SetRow(actionButtonsHost, 1);
        grid.Children.Add(actionButtonsHost);

        return grid;
    }

    private Grid BuildSettingsPanel()
    {
        var grid = new Grid
        {
            Visibility = Visibility.Collapsed
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var focusIntervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 10),
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
            Foreground = Brushes.White,
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
        Grid.SetRow(focusIntervalPanel, 0);
        grid.Children.Add(focusIntervalPanel);

        var breakIntervalPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
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
            Foreground = Brushes.White,
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
        Grid.SetRow(breakIntervalPanel, 1);
        grid.Children.Add(breakIntervalPanel);

        var soundPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12),
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
        _reminderSoundComboBox = CreateStyledComboBox();
        _reminderSoundComboBox.SelectionChanged += OnReminderSoundChanged;
        soundPanel.Children.Add(soundLabel);
        soundPanel.Children.Add(_reminderSoundComboBox);
        Grid.SetRow(soundPanel, 2);
        grid.Children.Add(soundPanel);

        var soundActionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _addCustomSoundButton = CreateSoundActionButton("添加音频");
        _addCustomSoundButton.Margin = new Thickness(0, 0, 8, 0);
        _addCustomSoundButton.Click += OnAddCustomSoundClick;
        soundActionPanel.Children.Add(_addCustomSoundButton);

        _removeCustomSoundButton = CreateSoundActionButton("删除当前自定义");
        _removeCustomSoundButton.Click += OnRemoveCurrentCustomSoundClick;
        soundActionPanel.Children.Add(_removeCustomSoundButton);

        Grid.SetRow(soundActionPanel, 3);
        grid.Children.Add(soundActionPanel);

        var hintText = new TextBlock
        {
            Text = "计时进行中不可修改时长",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 130, 135)),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        Grid.SetRow(hintText, 4);
        grid.Children.Add(hintText);

        return grid;
    }

    private static Border CreateIconButton(string icon, Action onClick)
    {
        var text = new TextBlock
        {
            Text = icon,
            Foreground = Brushes.White,
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var button = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = text
        };

        button.MouseEnter += (s, e) =>
        {
            button.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(62, 62, 70));
        };
        button.MouseLeave += (s, e) =>
        {
            button.Background = Brushes.Transparent;
        };
        button.MouseLeftButtonUp += (s, e) => onClick();

        return button;
    }

    private static WpfButton CreateSoundActionButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            Height = 28,
            MinWidth = 96,
            Padding = new Thickness(12, 2, 12, 2),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 68, 76)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 100)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
    }

    private static TextBlock CreateActionButtonText(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Border CreateActionButtonBorder(TextBlock textBlock, double width)
    {
        return new Border
        {
            Width = width,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Cursor = Cursors.Hand,
            Child = textBlock,
            Effect = new DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.35
            }
        };
    }

    private static Border CreateActionButton(string text, double width)
    {
        return CreateActionButtonBorder(CreateActionButtonText(text), width);
    }

    private static void ApplyActionButtonColor(Border button, System.Windows.Media.Color color)
    {
        button.Background = CreateBrush(color);
        if (button.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    private static void ConfigureActionButtonInteractions(
        Border button,
        Func<System.Windows.Media.Color> baseColorProvider,
        Func<System.Windows.Media.Color> hoverColorProvider,
        Func<System.Windows.Media.Color> pressedColorProvider,
        Action onClick)
    {
        button.MouseEnter += (s, e) => ApplyActionButtonColor(button, hoverColorProvider());
        button.MouseLeave += (s, e) => ApplyActionButtonColor(button, baseColorProvider());
        button.MouseLeftButtonDown += (s, e) => ApplyActionButtonColor(button, pressedColorProvider());
        button.MouseLeftButtonUp += (s, e) =>
        {
            onClick();
            ApplyActionButtonColor(button, baseColorProvider());
        };

        ApplyActionButtonColor(button, baseColorProvider());
    }

    private static ComboBox CreateStyledComboBox()
    {
        var combo = new ComboBox
        {
            Width = 160,
            Height = 28,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 240)),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 65, 74)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 100)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 2, 6, 2),
            FontSize = 12,
            MaxDropDownHeight = 220,
            IsEditable = false
        };

        // 瑕嗙洊绯荤粺榛樿涓嬫媺鑹诧紝閬垮厤鐧藉簳鐧藉瓧骞朵繚鎸佹繁鑹查鏍笺€?        combo.Resources[SystemColors.WindowBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 66));
        combo.Resources[SystemColors.ControlBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 66));
        combo.Resources[SystemColors.WindowTextBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 240));
        combo.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 89, 80));
        combo.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;

        var itemStyle = new Style(typeof(ComboBoxItem));
        itemStyle.Setters.Add(new Setter(WpfControl.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 240))));
        itemStyle.Setters.Add(new Setter(WpfControl.BackgroundProperty, Brushes.Transparent));
        itemStyle.Setters.Add(new Setter(WpfControl.BorderThicknessProperty, new Thickness(0)));
        itemStyle.Setters.Add(new Setter(WpfControl.PaddingProperty, new Thickness(8, 5, 8, 5)));
        itemStyle.Setters.Add(new Setter(WpfControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        itemStyle.Setters.Add(new Setter(WpfControl.VerticalContentAlignmentProperty, VerticalAlignment.Center));

        var highlightedTrigger = new Trigger
        {
            Property = ComboBoxItem.IsHighlightedProperty,
            Value = true
        };
        highlightedTrigger.Setters.Add(new Setter(WpfControl.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 90))));
        itemStyle.Triggers.Add(highlightedTrigger);

        var selectedTrigger = new Trigger
        {
            Property = ComboBoxItem.IsSelectedProperty,
            Value = true
        };
        selectedTrigger.Setters.Add(new Setter(WpfControl.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 89, 80))));
        selectedTrigger.Setters.Add(new Setter(WpfControl.ForegroundProperty, Brushes.White));
        itemStyle.Triggers.Add(selectedTrigger);

        combo.ItemContainerStyle = itemStyle;
        var templateXaml = """
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='ComboBox'>
    <Grid SnapsToDevicePixels='True'>
        <Border x:Name='MainBorder'
                CornerRadius='8'
                Background='{TemplateBinding Background}'
                BorderBrush='{TemplateBinding BorderBrush}'
                BorderThickness='{TemplateBinding BorderThickness}'/>

        <ContentPresenter x:Name='ContentSite'
                          Margin='10,0,30,0'
                          VerticalAlignment='Center'
                          HorizontalAlignment='Left'
                          IsHitTestVisible='False'
                          Content='{TemplateBinding SelectionBoxItem}'
                          ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                          ContentTemplateSelector='{TemplateBinding ItemTemplateSelector}'
                          TextElement.Foreground='{TemplateBinding Foreground}' />

        <Path x:Name='Arrow'
              Width='10'
              Height='6'
              Margin='0,0,10,0'
              HorizontalAlignment='Right'
              VerticalAlignment='Center'
              Fill='#D7D7DB'
              Data='M 0 0 L 5 6 L 10 0 Z' />

        <ToggleButton x:Name='ToggleButton'
                      Background='Transparent'
                      BorderThickness='0'
                      Focusable='False'
                      ClickMode='Press'
                      IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
            <ToggleButton.Template>
                <ControlTemplate TargetType='ToggleButton'>
                    <Border Background='Transparent'/>
                </ControlTemplate>
            </ToggleButton.Template>
        </ToggleButton>

        <Popup x:Name='Popup'
               Placement='Bottom'
               IsOpen='{TemplateBinding IsDropDownOpen}'
               Focusable='False'
               AllowsTransparency='True'
               PopupAnimation='Fade'>
            <Border Margin='0,4,0,0'
                    CornerRadius='8'
                    BorderThickness='1'
                    Background='#3A3A42'
                    BorderBrush='#5A5A64'>
                <ScrollViewer Margin='4' SnapsToDevicePixels='True'>
                    <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/>
                </ScrollViewer>
            </Border>
        </Popup>
    </Grid>
    <ControlTemplate.Triggers>
        <Trigger Property='IsMouseOver' Value='True'>
            <Setter TargetName='MainBorder' Property='BorderBrush' Value='#EF5950'/>
        </Trigger>
        <Trigger Property='IsKeyboardFocusWithin' Value='True'>
            <Setter TargetName='MainBorder' Property='BorderBrush' Value='#EF5950'/>
        </Trigger>
        <Trigger Property='IsDropDownOpen' Value='True'>
            <Setter TargetName='MainBorder' Property='BorderBrush' Value='#EF5950'/>
            <Setter TargetName='Arrow' Property='Fill' Value='#EF5950'/>
        </Trigger>
        <Trigger Property='IsEnabled' Value='False'>
            <Setter Property='Opacity' Value='0.55'/>
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>
""";
        combo.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(templateXaml);
        return combo;
    }

    private void UpdateViewVisibility()
    {
        _mainPanel.Visibility = _isSettingsView ? Visibility.Collapsed : Visibility.Visible;
        _settingsPanel.Visibility = _isSettingsView ? Visibility.Visible : Visibility.Collapsed;
        _openSettingsButton.Visibility = _isSettingsView ? Visibility.Collapsed : Visibility.Visible;
        _backButton.Visibility = _isSettingsView ? Visibility.Visible : Visibility.Collapsed;
        _todayFocusHeaderBorder.Visibility = _isSettingsView ? Visibility.Collapsed : Visibility.Visible;
        _headerTitle.Visibility = _isSettingsView ? Visibility.Visible : Visibility.Collapsed;
        if (_isSettingsView)
        {
            ResetTodayFocusHeaderVisualState();
        }
    }

    private void AnimateTodayFocusHeader(bool hovered)
    {
        if (_todayFocusHeaderScaleTransform == null ||
            _todayFocusHeaderTranslateTransform == null ||
            _todayFocusHintText == null ||
            _todayFocusLabelText == null ||
            _todayFocusValueText == null)
        {
            return;
        }

        var duration = TimeSpan.FromMilliseconds(240);
        var scaleTarget = hovered ? 1.1 : 1.0;
        var translateTarget = hovered ? 2.5 : 0.0;
        var easing = new CubicEase
        {
            EasingMode = hovered ? EasingMode.EaseOut : EasingMode.EaseIn
        };

        var scaleAnimation = new DoubleAnimation(scaleTarget, duration)
        {
            EasingFunction = easing
        };

        _todayFocusHeaderScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        _todayFocusHeaderScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

        var translateAnimation = new DoubleAnimation(translateTarget, duration)
        {
            EasingFunction = easing
        };
        _todayFocusHeaderTranslateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
        _todayFocusHeaderTranslateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);

        var labelSizeAnimation = new DoubleAnimation(hovered ? 13 : 12, duration)
        {
            EasingFunction = easing
        };
        _todayFocusLabelText.BeginAnimation(TextBlock.FontSizeProperty, labelSizeAnimation);

        var valueSizeAnimation = new DoubleAnimation(hovered ? 15.5 : 14, duration)
        {
            EasingFunction = easing
        };
        _todayFocusValueText.BeginAnimation(TextBlock.FontSizeProperty, valueSizeAnimation);

        if (hovered)
        {
            _todayFocusHintText.Visibility = Visibility.Visible;
        }

        var hintOpacityAnimation = new DoubleAnimation(hovered ? 1 : 0, duration)
        {
            EasingFunction = easing
        };
        _todayFocusHintText.BeginAnimation(UIElement.OpacityProperty, hintOpacityAnimation);

        var hintHeightAnimation = new DoubleAnimation(hovered ? 16 : 0, duration)
        {
            EasingFunction = easing
        };
        if (!hovered)
        {
            hintHeightAnimation.Completed += (s, e) =>
            {
                _todayFocusHintText.Visibility = Visibility.Collapsed;
            };
        }

        _todayFocusHintText.BeginAnimation(FrameworkElement.MaxHeightProperty, hintHeightAnimation);
    }

    private void ResetTodayFocusHeaderVisualState()
    {
        _todayFocusHeaderScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _todayFocusHeaderScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _todayFocusHeaderTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _todayFocusHeaderTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        _todayFocusLabelText.BeginAnimation(TextBlock.FontSizeProperty, null);
        _todayFocusValueText.BeginAnimation(TextBlock.FontSizeProperty, null);
        _todayFocusHintText.BeginAnimation(UIElement.OpacityProperty, null);
        _todayFocusHintText.BeginAnimation(FrameworkElement.MaxHeightProperty, null);

        _todayFocusHeaderScaleTransform.ScaleX = 1;
        _todayFocusHeaderScaleTransform.ScaleY = 1;
        _todayFocusHeaderTranslateTransform.X = 0;
        _todayFocusHeaderTranslateTransform.Y = 0;
        _todayFocusLabelText.FontSize = 12;
        _todayFocusValueText.FontSize = 14;
        _todayFocusHintText.Opacity = 0;
        _todayFocusHintText.MaxHeight = 0;
        _todayFocusHintText.Visibility = Visibility.Collapsed;
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
        _isLoadingSettings = false;
        RefreshReminderSoundItems(settings.ReminderSoundSelectionId);
    }

    private void OnFocusIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoadingSettings) return;

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
        if (_isLoadingSettings) return;

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

    private static ComboBoxItem CreateSoundItem(string label, string selectionId)
    {
        return new ComboBoxItem
        {
            Content = label,
            Tag = selectionId
        };
    }

    private static ComboBoxItem CreateSoundSeparatorItem()
    {
        return new ComboBoxItem
        {
            Content = "────────",
            IsEnabled = false,
            Focusable = false,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 126))
        };
    }

    private void RefreshReminderSoundItems(string? preferredSelectionId = null)
    {
        var settings = _settingsService.CurrentSettings;
        var targetSelectionId = string.IsNullOrWhiteSpace(preferredSelectionId)
            ? settings.ReminderSoundSelectionId
            : preferredSelectionId;

        _isLoadingSettings = true;
        _reminderSoundComboBox.Items.Clear();

        foreach (var (id, label) in BuiltinSoundOptions)
        {
            _reminderSoundComboBox.Items.Add(CreateSoundItem(label, id));
        }

        if (settings.CustomReminderSounds.Count > 0)
        {
            _reminderSoundComboBox.Items.Add(CreateSoundSeparatorItem());
            foreach (var customItem in settings.CustomReminderSounds.OrderBy(item => item.AddedAtUtc))
            {
                _reminderSoundComboBox.Items.Add(CreateSoundItem(customItem.DisplayName, customItem.Id));
            }
        }

        if (!SelectReminderSound(targetSelectionId))
        {
            SelectReminderSound(ReminderSoundPlayer.BuiltinAsteriskId);
            settings.ReminderSoundSelectionId = ReminderSoundPlayer.BuiltinAsteriskId;
            settings.ReminderSound = ReminderSoundType.Asterisk;
            _settingsService.SaveSettings();
        }

        _isLoadingSettings = false;
        UpdateCustomSoundButtonsState();
    }

    private bool SelectReminderSound(string? selectionId)
    {
        if (string.IsNullOrWhiteSpace(selectionId))
        {
            return false;
        }

        foreach (var item in _reminderSoundComboBox.Items)
        {
            if (item is ComboBoxItem comboItem &&
                comboItem.Tag is string itemId &&
                string.Equals(itemId, selectionId, StringComparison.OrdinalIgnoreCase))
            {
                _reminderSoundComboBox.SelectedItem = comboItem;
                _lastValidSoundSelectionId = itemId;
                return true;
            }
        }

        return false;
    }

    private void OnReminderSoundChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        if (_reminderSoundComboBox.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string selectedSelectionId)
        {
            _isLoadingSettings = true;
            SelectReminderSound(_lastValidSoundSelectionId ?? ReminderSoundPlayer.BuiltinAsteriskId);
            _isLoadingSettings = false;
            return;
        }

        var settings = _settingsService.CurrentSettings;
        settings.ReminderSoundSelectionId = selectedSelectionId;
        settings.ReminderSound = ReminderSoundPlayer.MapSelectionIdToLegacySound(selectedSelectionId);
        _lastValidSoundSelectionId = selectedSelectionId;
        _settingsService.SaveSettings();
        UpdateCustomSoundButtonsState();
        ReminderSoundPlayer.Play(settings);
    }

    private void UpdateCustomSoundButtonsState()
    {
        if (_removeCustomSoundButton == null || _reminderSoundComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        var isCustom = selectedItem.Tag is string selectionId &&
                       selectionId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase);
        _removeCustomSoundButton.IsEnabled = isCustom;
        _removeCustomSoundButton.Opacity = isCustom ? 1 : 0.55;
    }

    private void OnAddCustomSoundClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择提醒音频",
            Filter = "WAV 文件 (*.wav)|*.wav",
            CheckFileExists = true,
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        if (!string.Equals(IoPath.GetExtension(openFileDialog.FileName), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show(
                "仅支持 WAV 格式音频。",
                "添加音频",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var settings = _settingsService.CurrentSettings;
        try
        {
            var soundsDirectory = ReminderSoundPlayer.GetSoundsDirectory();
            Directory.CreateDirectory(soundsDirectory);

            var fileId = Guid.NewGuid().ToString("N");
            var originalName = IoPath.GetFileNameWithoutExtension(openFileDialog.FileName);
            var safeName = SanitizeFileName(originalName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "sound";
            }

            var targetFileName = $"{fileId}_{safeName}.wav";
            var targetPath = IoPath.Combine(soundsDirectory, targetFileName);
            File.Copy(openFileDialog.FileName, targetPath, overwrite: false);

            var customItem = new CustomReminderSoundItem
            {
                Id = $"custom:{fileId}",
                DisplayName = originalName,
                FileName = targetFileName,
                AddedAtUtc = DateTime.UtcNow
            };

            settings.CustomReminderSounds.Add(customItem);
            settings.ReminderSoundSelectionId = customItem.Id;
            settings.ReminderSound = ReminderSoundPlayer.MapSelectionIdToLegacySound(customItem.Id);
            _settingsService.SaveSettings();

            RefreshReminderSoundItems(customItem.Id);
            ReminderSoundPlayer.Play(settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderSound] Failed to add custom sound: {ex}");
            System.Windows.MessageBox.Show(
                "添加音频失败，请重试。",
                "添加音频",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnRemoveCurrentCustomSoundClick(object sender, RoutedEventArgs e)
    {
        if (_reminderSoundComboBox.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string selectionId ||
            !selectionId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        var customItem = settings.CustomReminderSounds
            .FirstOrDefault(item => string.Equals(item.Id, selectionId, StringComparison.OrdinalIgnoreCase));
        if (customItem == null)
        {
            settings.ReminderSoundSelectionId = ReminderSoundPlayer.BuiltinAsteriskId;
            settings.ReminderSound = ReminderSoundType.Asterisk;
            _settingsService.SaveSettings();
            RefreshReminderSoundItems(ReminderSoundPlayer.BuiltinAsteriskId);
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            $"确定删除音频“{customItem.DisplayName}”吗？",
            "删除音频",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        settings.CustomReminderSounds.Remove(customItem);
        settings.ReminderSoundSelectionId = ReminderSoundPlayer.BuiltinAsteriskId;
        settings.ReminderSound = ReminderSoundType.Asterisk;
        _settingsService.SaveSettings();

        TryDeleteCustomSoundFile(customItem.FileName);
        RefreshReminderSoundItems(ReminderSoundPlayer.BuiltinAsteriskId);
        ReminderSoundPlayer.Play(settings);
    }

    private static void TryDeleteCustomSoundFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        try
        {
            var filePath = IoPath.Combine(ReminderSoundPlayer.GetSoundsDirectory(), fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderSound] Failed to delete custom sound file: {ex}");
        }
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var invalidChars = IoPath.GetInvalidFileNameChars();
        var buffer = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            if (Array.IndexOf(invalidChars, ch) < 0)
            {
                buffer.Append(ch);
            }
        }

        var sanitized = buffer.ToString();
        if (sanitized.Length > 40)
        {
            sanitized = sanitized[..40];
        }

        return sanitized;
    }

    private void OnStartFocusClick()
    {
        if (_reminderService.IsRunning)
        {
            return;
        }

        ReminderSoundPlayer.Stop();
        _reminderService.StartFocus();
        UpdateDisplay();
    }

    private void OnStartBreakClick()
    {
        if (_reminderService.IsRunning)
        {
            return;
        }

        ReminderSoundPlayer.Stop();
        _reminderService.StartBreak();
        UpdateDisplay();
    }

    private void OnStopCurrentClick()
    {
        if (!_reminderService.IsRunning)
        {
            return;
        }

        _reminderService.Stop();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var remaining = _reminderService.GetRemainingTime();
        _timeText.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        UpdateProgress(remaining);
        UpdateTodayFocusTotalDisplay();

        var isRunning = _reminderService.IsRunning;
        _focusIntervalSlider.IsEnabled = !isRunning;
        _breakIntervalSlider.IsEnabled = !isRunning;
        UpdateActionButtonsState();
    }

    private void UpdateTodayFocusTotalDisplay()
    {
        var settings = _settingsService.CurrentSettings;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var persistedSeconds = settings.FocusTodayDate == today ? settings.FocusTodayCompletedSeconds : 0;

        var ongoingSeconds = 0;
        if (_reminderService.IsRunning &&
            _reminderService.CurrentMode == PomodoroMode.Focus &&
            _reminderService.CurrentSessionStartTimeLocal.HasValue)
        {
            var ongoingStart = _reminderService.CurrentSessionStartTimeLocal.Value;
            if (ongoingStart < todayStart)
            {
                ongoingStart = todayStart;
            }

            ongoingSeconds = (int)Math.Floor(Math.Max(0, (DateTime.Now - ongoingStart).TotalSeconds));
        }

        var totalSeconds = Math.Max(0, persistedSeconds + ongoingSeconds);
        var totalMinutes = totalSeconds / 60;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        _todayFocusValueText.Text = $"{hours:00}:{minutes:00}";
    }

    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    private void UpdateActionButtonsState()
    {
        var isRunning = _reminderService.IsRunning;
        var isBreakMode = _reminderService.CurrentMode == PomodoroMode.Break;

        _idleActionButtonsPanel.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
        _stopCurrentButtonBorder.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
        _stopCurrentButtonText.Text = isBreakMode ? "结束休息" : "结束专注";

        ApplyActionButtonColor(_startFocusButtonBorder, GetButtonBaseColor(isRunning: false, isBreakMode: false));
        ApplyActionButtonColor(_startBreakButtonBorder, GetButtonBaseColor(isRunning: false, isBreakMode: true));
        ApplyActionButtonColor(_stopCurrentButtonBorder, GetButtonBaseColor(isRunning: true, isBreakMode));
    }

    private static SolidColorBrush CreateBrush(System.Windows.Media.Color color)
    {
        return new SolidColorBrush(color);
    }

    private static System.Windows.Media.Color GetButtonBaseColor(bool isRunning, bool isBreakMode)
    {
        if (isBreakMode)
        {
            return isRunning
                ? System.Windows.Media.Color.FromRgb(76, 146, 106)
                : BreakColor.Color;
        }

        return isRunning
            ? System.Windows.Media.Color.FromRgb(130, 65, 65)
            : TomatoColor.Color;
    }

    private static System.Windows.Media.Color GetButtonHoverColor(bool isRunning, bool isBreakMode)
    {
        if (isBreakMode)
        {
            return isRunning
                ? System.Windows.Media.Color.FromRgb(88, 166, 120)
                : System.Windows.Media.Color.FromRgb(130, 210, 156);
        }

        return isRunning
            ? System.Windows.Media.Color.FromRgb(160, 80, 80)
            : System.Windows.Media.Color.FromRgb(255, 110, 100);
    }

    private static System.Windows.Media.Color GetButtonPressedColor(bool isRunning, bool isBreakMode)
    {
        if (isBreakMode)
        {
            return isRunning
                ? System.Windows.Media.Color.FromRgb(64, 128, 92)
                : System.Windows.Media.Color.FromRgb(88, 166, 120);
        }

        return isRunning
            ? System.Windows.Media.Color.FromRgb(120, 55, 55)
            : TomatoDarkColor.Color;
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




