using System;
using System.Windows;
using System.Windows.Controls;
using ModernWpf;
using ModernWpf.Controls;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class UiSettingsPage : System.Windows.Controls.Page
    {
        private readonly ConfigManager _configManager;

        public UiSettingsPage(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 主题设置
            var theme = _configManager.UITheme;
            SetThemeSelection(theme);
            
            // 频谱显示设置
            ShowWaveformCheckBox.IsChecked = _configManager.ShowWaveform;
            
            // 窗口行为设置
            MinimizeToTrayCheckBox.IsChecked = _configManager.MinimizeToTray;
            
            UpdateWaveformSettingsVisibility();
        }
        
        private void SetThemeSelection(string theme)
        {
            for (int i = 0; i < ThemeComboBox.Items.Count; i++)
            {
                if (ThemeComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == theme)
                {
                    ThemeComboBox.SelectedIndex = i;
                    return;
                }
            }
            ThemeComboBox.SelectedIndex = 0; // 默认浅色
        }
        

        public void SaveSettings()
        {
            // 准备批量保存的设置
            var settings = new System.Collections.Generic.Dictionary<string, string>
            {
                ["VoiceInput:UI:Theme"] = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Light",
                ["VoiceInput:UI:ShowWaveform"] = (ShowWaveformCheckBox.IsChecked ?? false).ToString(),
                ["VoiceInput:UI:MinimizeToTray"] = (MinimizeToTrayCheckBox.IsChecked ?? false).ToString()
            };
            
            // 批量保存所有UI设置
            _configManager.SaveSettingsBatch(settings);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                switch (item.Tag?.ToString())
                {
                    case "Light":
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        break;
                    case "Dark":
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        break;
                    case "System":
                        ThemeManager.Current.ApplicationTheme = null;
                        break;
                }
            }
        }

        private void ShowWaveformCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateWaveformSettingsVisibility();
        }

        private void UpdateWaveformSettingsVisibility()
        {
            if (WaveformSettingsPanel != null)
            {
                WaveformSettingsPanel.Visibility = (ShowWaveformCheckBox.IsChecked ?? false) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }
    }
}