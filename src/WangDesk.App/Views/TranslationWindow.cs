using System.Windows;
using System.Windows.Controls;
using WangDesk.App.Services;

namespace WangDesk.App.Views;

/// <summary>
/// 翻译窗口
/// </summary>
public partial class TranslationWindow : Window
{
    private readonly ITranslationService _translationService;

    public TranslationWindow(ITranslationService translationService)
    {
        _translationService = translationService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "翻译 - 旺旺桌宠";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new global::System.Windows.Controls.Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 标题
        var header = new global::System.Windows.Controls.TextBlock
        {
            Text = "百度翻译",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 15)
        };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // 说明文字
        var description = new global::System.Windows.Controls.TextBlock
        {
            Text = _translationService.IsConfigured ? "支持中英文自动互译" : "请先配置百度翻译API密钥",
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(description, 1);
        grid.Children.Add(description);

        // 输入文本框
        _inputTextBox = new global::System.Windows.Controls.TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(_inputTextBox, 2);
        grid.Children.Add(_inputTextBox);

        // 翻译按钮
        var buttonPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var translateButton = new global::System.Windows.Controls.Button { Content = "翻译", Width = 100, Height = 30 };
        translateButton.Click += OnTranslateClick;
        var clearButton = new global::System.Windows.Controls.Button { Content = "清空", Width = 100, Height = 30, Margin = new Thickness(10, 0, 0, 0) };
        clearButton.Click += (s, e) =>
        {
            _inputTextBox.Clear();
            _outputTextBox.Clear();
        };
        buttonPanel.Children.Add(translateButton);
        buttonPanel.Children.Add(clearButton);
        Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        // 输出文本框
        _outputTextBox = new global::System.Windows.Controls.TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = System.Windows.Media.Brushes.LightGray
        };
        Grid.SetRow(_outputTextBox, 4);
        grid.Children.Add(_outputTextBox);

        // 关闭按钮
        var closeButtonPanel = new global::System.Windows.Controls.StackPanel { Orientation = global::System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var closeButton = new global::System.Windows.Controls.Button { Content = "关闭", Width = 80, Height = 30 };
        closeButton.Click += (s, e) => Close();
        closeButtonPanel.Children.Add(closeButton);
        Grid.SetRow(closeButtonPanel, 5);
        grid.Children.Add(closeButtonPanel);

        Content = grid;
    }

    private global::System.Windows.Controls.TextBox _inputTextBox = null!;
    private global::System.Windows.Controls.TextBox _outputTextBox = null!;

    private async void OnTranslateClick(object sender, RoutedEventArgs e)
    {
        var input = _inputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            global::System.Windows.MessageBox.Show("请输入需要翻译的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!_translationService.IsConfigured)
        {
            global::System.Windows.MessageBox.Show("请先配置百度翻译API密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _outputTextBox.Text = "翻译中...";
        var result = await _translationService.TranslateAsync(input);
        _outputTextBox.Text = result;
    }
}
