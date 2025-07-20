using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class TranscriptionTranslationPage : Page
    {
        private readonly ConfigManager _configManager;
        private readonly ILoggerService _logger;
        private readonly SecureStorageService _secureStorage;
        private readonly ITextToSpeechService _ttsService;

        public TranscriptionTranslationPage()
        {
            InitializeComponent();
            
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            
            _configManager = serviceProvider?.GetService<ConfigManager>();
            _logger = serviceProvider?.GetService<ILoggerService>();
            _secureStorage = serviceProvider?.GetService<SecureStorageService>();
            _ttsService = serviceProvider?.GetService<ITextToSpeechService>();
            
            if (_configManager != null)
            {
                DataContext = _configManager;
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            try
            {
                // 加载 Whisper 设置
                WhisperModelComboBox.SelectedValue = _configManager.WhisperModel;
                WhisperApiUrlBox.Text = _configManager.WhisperBaseUrl;
                WhisperTimeoutBox.Value = _configManager.WhisperTimeout;
                WhisperTemperatureSlider.Value = _configManager.WhisperTemperature;
                ResponseFormatComboBox.SelectedValue = _configManager.WhisperResponseFormat;
                
                // 如果有保存的 Whisper API 密钥，显示星号
                if (_secureStorage?.HasWhisperApiKey() == true || _secureStorage?.HasApiKey() == true)
                {
                    WhisperApiKeyBox.Password = new string('●', 8);
                }
                
                // 加载 GPT 设置
                GPTModelComboBox.SelectedValue = _configManager.GPTModel;
                GPTApiUrlBox.Text = _configManager.GPTBaseUrl;
                GPTTimeoutBox.Value = _configManager.GPTTimeout;
                GPTTemperatureSlider.Value = _configManager.GPTTemperature;
                MaxTokensBox.Value = _configManager.GPTMaxTokens;
                
                // 如果有保存的 GPT API 密钥，显示星号
                if (_secureStorage?.HasGPTApiKey() == true || _secureStorage?.HasApiKey() == true)
                {
                    GPTApiKeyBox.Password = new string('●', 8);
                }
                
                // 加载 TTS 设置
                LoadTtsSettings();
            }
            catch (Exception ex)
            {
                _logger?.Error($"加载转写翻译设置失败: {ex.Message}", ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                // 保存 Whisper 设置
                if (WhisperModelComboBox.SelectedValue != null)
                {
                    _configManager.SaveSetting("VoiceInput:WhisperAPI:Model", WhisperModelComboBox.SelectedValue.ToString());
                }
                _configManager.SaveSetting("VoiceInput:WhisperAPI:BaseUrl", WhisperApiUrlBox.Text);
                _configManager.SaveSetting("VoiceInput:WhisperAPI:Timeout", WhisperTimeoutBox.Value.ToString());
                _configManager.SaveSetting("VoiceInput:WhisperAPI:Temperature", WhisperTemperatureSlider.Value.ToString());
                if (ResponseFormatComboBox.SelectedValue != null)
                {
                    _configManager.SaveSetting("VoiceInput:WhisperAPI:ResponseFormat", ResponseFormatComboBox.SelectedValue.ToString());
                }
                
                // 保存 GPT 设置
                if (GPTModelComboBox.SelectedValue != null)
                {
                    _configManager.SaveSetting("VoiceInput:GPTAPI:Model", GPTModelComboBox.SelectedValue.ToString());
                }
                _configManager.SaveSetting("VoiceInput:GPTAPI:BaseUrl", GPTApiUrlBox.Text);
                _configManager.SaveSetting("VoiceInput:GPTAPI:Timeout", GPTTimeoutBox.Value.ToString());
                _configManager.SaveSetting("VoiceInput:GPTAPI:Temperature", GPTTemperatureSlider.Value.ToString());
                _configManager.SaveSetting("VoiceInput:GPTAPI:MaxTokens", MaxTokensBox.Value.ToString());
                
                // 保存 TTS 设置
                SaveTtsSettings();
                
                _logger?.Info("转写翻译设置已保存");
            }
            catch (Exception ex)
            {
                _logger?.Error($"保存转写翻译设置失败: {ex.Message}", ex);
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Whisper API 密钥事件处理
        private void ShowWhisperApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = _secureStorage?.LoadWhisperApiKey() ?? _secureStorage?.LoadApiKey();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    WhisperApiKeyBox.Password = apiKey;
                    _logger?.Info("显示 Whisper API 密钥");
                }
                else
                {
                    MessageBox.Show("未设置 API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示 Whisper API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"显示密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveWhisperApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = WhisperApiKeyBox.Password;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("请输入 API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                _secureStorage?.SaveWhisperApiKey(apiKey);
                // 显示为星号而不是清空
                WhisperApiKeyBox.Password = new string('●', 8);
                
                MessageBox.Show("Whisper API 密钥已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _logger?.Info("Whisper API 密钥已保存");
            }
            catch (Exception ex)
            {
                _logger?.Error($"保存 Whisper API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"保存密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // GPT API 密钥事件处理
        private void ShowGPTApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = _secureStorage?.LoadGPTApiKey() ?? _secureStorage?.LoadApiKey();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    GPTApiKeyBox.Password = apiKey;
                    _logger?.Info("显示 GPT API 密钥");
                }
                else
                {
                    MessageBox.Show("未设置 API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示 GPT API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"显示密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveGPTApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = GPTApiKeyBox.Password;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("请输入 API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                _secureStorage?.SaveGPTApiKey(apiKey);
                // 显示为星号而不是清空
                GPTApiKeyBox.Password = new string('●', 8);
                
                MessageBox.Show("GPT API 密钥已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _logger?.Info("GPT API 密钥已保存");
            }
            catch (Exception ex)
            {
                _logger?.Error($"保存 GPT API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"保存密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // TTS 设置相关方法
        private void LoadTtsSettings()
        {
            if (_configManager?.TtsSettings == null) return;
            
            var ttsSettings = _configManager.TtsSettings;
            
            // 加载 CheckBox 状态
            UseOpenAIConfigCheckBox.IsChecked = ttsSettings.UseOpenAIConfig;
            
            // 加载自定义 API 设置
            if (!ttsSettings.UseOpenAIConfig && !string.IsNullOrEmpty(ttsSettings.CustomApiKey))
            {
                TtsApiKeyBox.Password = new string('●', 8);
            }
            TtsApiUrlBox.Text = ttsSettings.CustomApiUrl;
            
            // 加载模型和参数
            TtsModelComboBox.SelectedValue = ttsSettings.Model;
            TtsSpeedSlider.Value = ttsSettings.Speed;
            TtsVolumeSlider.Value = ttsSettings.Volume;
            
            // 加载语音映射
            if (ttsSettings.VoiceMapping != null)
            {
                if (ttsSettings.VoiceMapping.ContainsKey("zh"))
                    ChineseVoiceComboBox.SelectedValue = ttsSettings.VoiceMapping["zh"];
                if (ttsSettings.VoiceMapping.ContainsKey("en"))
                    EnglishVoiceComboBox.SelectedValue = ttsSettings.VoiceMapping["en"];
                if (ttsSettings.VoiceMapping.ContainsKey("ja"))
                    JapaneseVoiceComboBox.SelectedValue = ttsSettings.VoiceMapping["ja"];
                if (ttsSettings.VoiceMapping.ContainsKey("default"))
                    DefaultVoiceComboBox.SelectedValue = ttsSettings.VoiceMapping["default"];
            }
        }
        
        private void SaveTtsSettings()
        {
            if (_configManager?.TtsSettings == null) return;
            
            var ttsSettings = _configManager.TtsSettings;
            
            // 保存 UseOpenAIConfig
            ttsSettings.UseOpenAIConfig = UseOpenAIConfigCheckBox.IsChecked ?? true;
            
            // 如果使用自定义配置，保存密钥
            if (!ttsSettings.UseOpenAIConfig && !string.IsNullOrWhiteSpace(TtsApiKeyBox.Password) && !TtsApiKeyBox.Password.StartsWith("●"))
            {
                ttsSettings.CustomApiKey = TtsApiKeyBox.Password;
            }
            
            // 保存自定义 API URL
            ttsSettings.CustomApiUrl = TtsApiUrlBox.Text;
            
            // 保存模型和参数
            ttsSettings.Model = TtsModelComboBox.SelectedValue?.ToString() ?? "tts-1";
            ttsSettings.Speed = TtsSpeedSlider.Value;
            ttsSettings.Volume = (int)TtsVolumeSlider.Value;
            
            // 保存语音映射
            ttsSettings.VoiceMapping["zh"] = ChineseVoiceComboBox.SelectedValue?.ToString() ?? "nova";
            ttsSettings.VoiceMapping["en"] = EnglishVoiceComboBox.SelectedValue?.ToString() ?? "echo";
            ttsSettings.VoiceMapping["ja"] = JapaneseVoiceComboBox.SelectedValue?.ToString() ?? "alloy";
            ttsSettings.VoiceMapping["default"] = DefaultVoiceComboBox.SelectedValue?.ToString() ?? "nova";
            
            // 保存到配置文件
            _configManager.SaveTtsSettings(ttsSettings);
        }
        
        // TTS API 密钥事件处理
        private void ShowTtsApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ttsSettings = _configManager?.TtsSettings;
                if (ttsSettings != null && !string.IsNullOrEmpty(ttsSettings.CustomApiKey))
                {
                    TtsApiKeyBox.Password = ttsSettings.CustomApiKey;
                    _logger?.Info("显示 TTS API 密钥");
                }
                else
                {
                    MessageBox.Show("未设置 TTS API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示 TTS API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"显示密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveTtsApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = TtsApiKeyBox.Password;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("请输入 API 密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (_configManager?.TtsSettings != null)
                {
                    _configManager.TtsSettings.CustomApiKey = apiKey;
                    _configManager.SaveTtsSettings(_configManager.TtsSettings);
                }
                
                // 显示为星号而不是清空
                TtsApiKeyBox.Password = new string('●', 8);
                
                MessageBox.Show("TTS API 密钥已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _logger?.Info("TTS API 密钥已保存");
            }
            catch (Exception ex)
            {
                _logger?.Error($"保存 TTS API 密钥失败: {ex.Message}", ex);
                MessageBox.Show($"保存密钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}