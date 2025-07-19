using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using VoiceInput.Core;
using VoiceInput.Services;
using VoiceInput.Views.Pages;

namespace VoiceInput.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigManager _configManager;
        private BasicSettingsPage _basicPage;
        private TranscriptionTranslationPage _transcriptionTranslationPage;
        private ProxySettingsPage _proxyPage;
        private UiSettingsPage _uiPage;
        private HotkeyProfilesPage _hotkeyProfilesPage;

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
            _transcriptionTranslationPage = new TranscriptionTranslationPage();
            _proxyPage = new ProxySettingsPage(_configManager);
            _uiPage = new UiSettingsPage(_configManager);
            _hotkeyProfilesPage = new HotkeyProfilesPage();
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
                    case "TranscriptionTranslation":
                        ContentFrame.Navigate(_transcriptionTranslationPage);
                        break;
                    case "Proxy":
                        ContentFrame.Navigate(_proxyPage);
                        break;
                    case "UI":
                        ContentFrame.Navigate(_uiPage);
                        break;
                    case "HotkeyProfiles":
                        ContentFrame.Navigate(_hotkeyProfilesPage);
                        break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存热键之前的值
                var oldHotkey = _configManager.Hotkey;
                
                // 保存所有页面的设置
                _basicPage.SaveSettings();
                _transcriptionTranslationPage.SaveSettings();
                _proxyPage.SaveSettings();
                _uiPage.SaveSettings();
                
                // 如果热键有变化，更新全局热键服务
                var newHotkey = _configManager.Hotkey;
                if (oldHotkey != newHotkey)
                {
                    try
                    {
                        var serviceProvider = (Application.Current as App)?.GetServiceProvider();
                        if (serviceProvider != null)
                        {
                            var controller = serviceProvider.GetRequiredService<VoiceInputController>();
                            controller.UpdateHotkey(newHotkey);
                            LoggerService.Log($"热键已更新: {oldHotkey} -> {newHotkey}");
                        }
                    }
                    catch (Exception hotkeyEx)
                    {
                        MessageBox.Show($"更新热键失败: {hotkeyEx.Message}\n\n热键将在下次启动时生效。", 
                            "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

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