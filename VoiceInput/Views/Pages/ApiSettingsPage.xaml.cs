using System;
using System.Windows.Controls;
using ModernWpf.Controls;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class ApiSettingsPage : System.Windows.Controls.Page
    {
        private readonly ConfigManager _configManager;

        public ApiSettingsPage(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载模型设置
            ModelComboBox.SelectedIndex = 0; // 目前只有 whisper-1

            // 加载 API URL
            ApiUrlBox.Text = _configManager.WhisperBaseUrl;

            // 加载超时设置
            TimeoutBox.Value = _configManager.WhisperTimeout;

            // TODO: 加载语言设置（需要扩展ConfigManager）
            // 暂时使用默认值
            InputLanguageComboBox.SelectedIndex = 0; // 自动检测
            OutputModeComboBox.SelectedIndex = 0; // 原语言转录
            
            // TODO: 加载Temperature设置（需要扩展ConfigManager）
            TemperatureBox.Value = 0.0;
        }

        public void SaveSettings()
        {
            // 保存模型（目前只支持whisper-1，所以暂时不保存）
            
            // 保存API URL
            var apiUrl = ApiUrlBox.Text.Trim();
            if (!string.IsNullOrEmpty(apiUrl))
            {
                // TODO: 需要在ConfigManager中添加保存WhisperBaseUrl的方法
                // _configManager.SaveWhisperBaseUrl(apiUrl);
            }

            // 保存超时设置
            if (!double.IsNaN(TimeoutBox.Value))
            {
                // TODO: 需要在ConfigManager中添加保存WhisperTimeout的方法
                // _configManager.SaveWhisperTimeout((int)TimeoutBox.Value);
            }

            // TODO: 保存语言设置
            var selectedLanguage = (InputLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var selectedMode = (OutputModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            
            // TODO: 保存Temperature设置
            if (!double.IsNaN(TemperatureBox.Value))
            {
                // _configManager.SaveTemperature(TemperatureBox.Value);
            }
        }
    }
}