using System.Windows;
using System.Windows.Controls;
using WangDesk.App.Services;

namespace WangDesk.App.Views;

/// <summary>
/// 设置窗口
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IAutoStartService _autoStartService;
    private readonly IReminderService _reminderService;

    public SettingsWindow(
        ISettingsService settingsService,
        IAutoStartService autoStartService,
        IReminderService reminderService)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _reminderService = reminderService;

        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Title = "设置 - 旺旺桌宠";
        Width = 450;
        Height = 350;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new global::System.Windows.Controls.Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 百度翻译设置
        var translateHeader = new global::System.Windows.Controls.TextBlock
        {
            Text = "百度翻译设置",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(translateHeader, 0);
        grid.Children.Add(translateHeader);

        var appIdPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
        var appIdLabel = new global::System.Windows.Controls.TextBlock { Text = "App ID:", Width = 100, VerticalAlignment = VerticalAlignment.Center };
        _appIdTextBox = new global::System.Windows.Controls.TextBox { Width = 280, Height = 25 };
        appIdPanel.Children.Add(appIdLabel);
        appIdPanel.Children.Add(_appIdTextBox);
        Grid.SetRow(appIdPanel, 1);
        grid.Children.Add(appIdPanel);

        var secretKeyPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        var secretKeyLabel = new global::System.Windows.Controls.TextBlock { Text = "Secret Key:", Width = 100, VerticalAlignment = VerticalAlignment.Center };
        _secretKeyTextBox = new global::System.Windows.Controls.TextBox { Width = 280, Height = 25 };
        secretKeyPanel.Children.Add(secretKeyLabel);
        secretKeyPanel.Children.Add(_secretKeyTextBox);
        Grid.SetRow(secretKeyPanel, 2);
        grid.Children.Add(secretKeyPanel);

        // 定时提醒设置
        var reminderHeader = new global::System.Windows.Controls.TextBlock
        {
            Text = "定时提醒设置",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(reminderHeader, 3);
        grid.Children.Add(reminderHeader);

        var intervalPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
        var intervalLabel = new global::System.Windows.Controls.TextBlock { Text = "提醒间隔:", Width = 100, VerticalAlignment = VerticalAlignment.Center };
        _intervalSlider = new global::System.Windows.Controls.Slider { Width = 200, Minimum = 5, Maximum = 120, Value = 45, TickFrequency = 5, IsSnapToTickEnabled = true };
        _intervalValueLabel = new global::System.Windows.Controls.TextBlock { Text = "45 分钟", Width = 60, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        _intervalSlider.ValueChanged += (s, e) => _intervalValueLabel.Text = $"{(int)_intervalSlider.Value} 分钟";
        intervalPanel.Children.Add(intervalLabel);
        intervalPanel.Children.Add(_intervalSlider);
        intervalPanel.Children.Add(_intervalValueLabel);
        Grid.SetRow(intervalPanel, 4);
        grid.Children.Add(intervalPanel);

        // 开机自启设置
        var autoStartPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 20) };
        _autoStartCheckBox = new global::System.Windows.Controls.CheckBox { Content = "开机自动启动", VerticalAlignment = VerticalAlignment.Center };
        autoStartPanel.Children.Add(_autoStartCheckBox);
        Grid.SetRow(autoStartPanel, 5);
        grid.Children.Add(autoStartPanel);

        // 按钮
        var buttonPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right };
        var saveButton = new global::System.Windows.Controls.Button { Content = "保存", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
        saveButton.Click += OnSaveClick;
        var cancelButton = new global::System.Windows.Controls.Button { Content = "取消", Width = 80, Height = 30 };
        cancelButton.Click += (s, e) => Close();
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 6);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }

    private global::System.Windows.Controls.TextBox _appIdTextBox = null!;
    private global::System.Windows.Controls.TextBox _secretKeyTextBox = null!;
    private global::System.Windows.Controls.Slider _intervalSlider = null!;
    private global::System.Windows.Controls.TextBlock _intervalValueLabel = null!;
    private global::System.Windows.Controls.CheckBox _autoStartCheckBox = null!;

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        _appIdTextBox.Text = settings.BaiduTranslateAppId;
        _secretKeyTextBox.Text = settings.BaiduTranslateSecretKey;
        _intervalSlider.Value = settings.ReminderIntervalMinutes;
        _intervalValueLabel.Text = $"{settings.ReminderIntervalMinutes} 分钟";
        _autoStartCheckBox.IsChecked = settings.AutoStartEnabled;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        settings.BaiduTranslateAppId = _appIdTextBox.Text.Trim();
        settings.BaiduTranslateSecretKey = _secretKeyTextBox.Text.Trim();
        settings.ReminderIntervalMinutes = (int)_intervalSlider.Value;
        settings.AutoStartEnabled = _autoStartCheckBox.IsChecked ?? false;

        _settingsService.SaveSettings();
        Close();
    }
}
