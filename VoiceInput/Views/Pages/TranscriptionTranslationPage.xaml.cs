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

        public TranscriptionTranslationPage()
        {
            InitializeComponent();
            
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            
            _configManager = serviceProvider?.GetService<ConfigManager>();
            _logger = serviceProvider?.GetService<ILoggerService>();
            _secureStorage = serviceProvider?.GetService<SecureStorageService>();
            
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
    }
}