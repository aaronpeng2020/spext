using System;
using System.Windows;
using ModernWpf.Controls;
using VoiceInput.Services;
using VoiceInput.Views.Pages;

namespace VoiceInput.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigManager _configManager;
        private BasicSettingsPage _basicPage;
        private ApiSettingsPage _apiPage;
        private ProxySettingsPage _proxyPage;
        private UiSettingsPage _uiPage;

        public SettingsWindow(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            
            InitializePages();
            
            // 默认显示基本设置页面
            ContentFrame.Navigate(_basicPage);
        }

        private void InitializePages()
        {
            // 创建页面实例并传递配置管理器
            _basicPage = new BasicSettingsPage(_configManager);
            _apiPage = new ApiSettingsPage(_configManager);
            _proxyPage = new ProxySettingsPage(_configManager);
            _uiPage = new UiSettingsPage(_configManager);
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag?.ToString())
                {
                    case "Basic":
                        ContentFrame.Navigate(_basicPage);
                        break;
                    case "API":
                        ContentFrame.Navigate(_apiPage);
                        break;
                    case "Proxy":
                        ContentFrame.Navigate(_proxyPage);
                        break;
                    case "UI":
                        ContentFrame.Navigate(_uiPage);
                        break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存所有页面的设置
                _basicPage.SaveSettings();
                _apiPage.SaveSettings();
                _proxyPage.SaveSettings();
                _uiPage.SaveSettings();

                MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}