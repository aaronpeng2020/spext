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
            // TODO: 从ConfigManager加载UI设置
            // 暂时使用默认值
            
            // 主题设置
            var currentTheme = ThemeManager.Current.ActualApplicationTheme;
            ThemeComboBox.SelectedIndex = currentTheme == ApplicationTheme.Dark ? 1 : 0;
            
            // 波形显示设置
            ShowWaveformCheckBox.IsChecked = true; // TODO: 从配置加载
            WaveformHeightBox.Value = 100; // TODO: 从配置加载
            WaveformColorComboBox.SelectedIndex = 0; // TODO: 从配置加载
            
            // 窗口行为设置
            MinimizeToTrayCheckBox.IsChecked = true; // TODO: 从配置加载
            
            UpdateWaveformSettingsVisibility();
        }

        public void SaveSettings()
        {
            // TODO: 保存UI设置到ConfigManager
            
            // 保存主题设置
            var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            // TODO: _configManager.SaveTheme(selectedTheme);
            
            // 保存波形显示设置
            var showWaveform = ShowWaveformCheckBox.IsChecked ?? false;
            // TODO: _configManager.SaveShowWaveform(showWaveform);
            
            if (showWaveform)
            {
                // 保存波形高度
                if (!double.IsNaN(WaveformHeightBox.Value))
                {
                    // TODO: _configManager.SaveWaveformHeight((int)WaveformHeightBox.Value);
                }
                
                // 保存波形颜色
                var selectedColor = (WaveformColorComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                // TODO: _configManager.SaveWaveformColor(selectedColor);
            }
            
            // 保存窗口行为设置
            var minimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
            // TODO: _configManager.SaveMinimizeToTray(minimizeToTray);
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