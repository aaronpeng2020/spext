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

            // 加载语言设置
            SetLanguageSelection(_configManager.WhisperLanguage);
            
            // 加载输出模式
            SetOutputModeSelection(_configManager.WhisperOutputMode);
            
            // 加载Temperature设置
            TemperatureBox.Value = _configManager.WhisperTemperature;
        }
        
        private void SetLanguageSelection(string language)
        {
            for (int i = 0; i < InputLanguageComboBox.Items.Count; i++)
            {
                if (InputLanguageComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == language)
                {
                    InputLanguageComboBox.SelectedIndex = i;
                    return;
                }
            }
            InputLanguageComboBox.SelectedIndex = 0; // 默认自动检测
        }
        
        private void SetOutputModeSelection(string mode)
        {
            for (int i = 0; i < OutputModeComboBox.Items.Count; i++)
            {
                if (OutputModeComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == mode)
                {
                    OutputModeComboBox.SelectedIndex = i;
                    return;
                }
            }
            OutputModeComboBox.SelectedIndex = 0; // 默认原语言转录
        }

        public void SaveSettings()
        {
            // 保存API URL
            var apiUrl = ApiUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(apiUrl))
            {
                apiUrl = "https://api.openai.com/v1/audio/transcriptions";
            }
            
            // 保存超时设置
            int timeout = 30;
            if (!double.IsNaN(TimeoutBox.Value))
            {
                timeout = (int)TimeoutBox.Value;
            }
            
            // 保存语言设置
            var selectedLanguage = (InputLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
            var selectedMode = (OutputModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "transcription";
            
            // 保存Temperature设置
            double temperature = 0.0;
            if (!double.IsNaN(TemperatureBox.Value))
            {
                temperature = TemperatureBox.Value;
            }
            
            // 保存所有Whisper设置
            _configManager.SaveWhisperSettings(apiUrl, timeout, selectedLanguage, selectedMode, temperature);
        }
    }
}