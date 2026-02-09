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

    public SettingsWindow(
        ISettingsService settingsService,
        IAutoStartService autoStartService)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;

        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Title = "设置 - 旺旺桌宠";
        Width = 420;
        Height = 220;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new global::System.Windows.Controls.Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 开机自启设置
        var autoStartPanel = new global::System.Windows.Controls.StackPanel
        {
            Orientation = global::System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 20)
        };
        _autoStartCheckBox = new global::System.Windows.Controls.CheckBox
        {
            Content = "开机自动启动",
            VerticalAlignment = VerticalAlignment.Center
        };
        autoStartPanel.Children.Add(_autoStartCheckBox);
        Grid.SetRow(autoStartPanel, 0);
        grid.Children.Add(autoStartPanel);

        // 按钮
        var buttonPanel = new global::System.Windows.Controls.StackPanel
        {
            Orientation = global::System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right
        };
        var saveButton = new global::System.Windows.Controls.Button
        {
            Content = "保存",
            Width = 80,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0)
        };
        saveButton.Click += OnSaveClick;
        var cancelButton = new global::System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 80,
            Height = 30
        };
        cancelButton.Click += (s, e) => Close();
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }

    private global::System.Windows.Controls.CheckBox _autoStartCheckBox = null!;

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        _autoStartCheckBox.IsChecked = settings.AutoStartEnabled;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        settings.AutoStartEnabled = _autoStartCheckBox.IsChecked ?? false;

        _settingsService.SaveSettings();
        _autoStartService.SetAutoStart(settings.AutoStartEnabled);
        Close();
    }
}
