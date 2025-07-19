using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoiceInput.Services
{
    public class ConfigManager
    {
        private readonly IConfiguration _configuration;
        private readonly SecureStorageService _secureStorage;
        private readonly AutoStartService _autoStartService;
        private string? _cachedApiKey;
        
        public ConfigManager(IConfiguration configuration, SecureStorageService secureStorage, AutoStartService autoStartService)
        {
            _configuration = configuration;
            _secureStorage = secureStorage;
            _autoStartService = autoStartService;
            LoadApiKey();
            LoadProxyCredentials();
            
            // 同步自动启动设置
            if (AutoStart != _autoStartService.IsAutoStartEnabled())
            {
                _autoStartService.UpdateAutoStart(AutoStart);
            }
        }

        public void ReloadConfiguration()
        {
            // 重新加载配置
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                LoggerService.Log("配置已重新加载");
                
                // 重新加载缓存的凭据，避免凭据丢失
                LoadApiKey();
                LoadProxyCredentials();
            }
        }

        public string Hotkey => _configuration["VoiceInput:Hotkey"] ?? "F3";
        
        public bool AutoStart 
        { 
            get => bool.TryParse(_configuration["VoiceInput:AutoStart"], out var result) && result;
            set
            {
                SaveSetting("VoiceInput:AutoStart", value.ToString());
                _autoStartService.UpdateAutoStart(value);
            }
        }

        public bool MuteWhileRecording
        {
            get => bool.TryParse(_configuration["VoiceInput:MuteWhileRecording"], out var result) && result;
            set => SaveSetting("VoiceInput:MuteWhileRecording", value.ToString());
        }

        public int AudioSampleRate => int.TryParse(_configuration["VoiceInput:AudioSettings:SampleRate"], out var rate) ? rate : 16000;
        
        public int AudioBitsPerSample => int.TryParse(_configuration["VoiceInput:AudioSettings:BitsPerSample"], out var bits) ? bits : 16;
        
        public int AudioChannels => int.TryParse(_configuration["VoiceInput:AudioSettings:Channels"], out var channels) ? channels : 1;

        public string WhisperModel => _configuration["VoiceInput:WhisperAPI:Model"] ?? "whisper-1";
        
        public string WhisperBaseUrl => _configuration["VoiceInput:WhisperAPI:BaseUrl"] ?? "https://api.openai.com/v1/audio/transcriptions";
        
        // GPT API 配置
        public string GPTModel => _configuration["VoiceInput:GPTAPI:Model"] ?? "gpt-4o-mini";
        
        public string GPTBaseUrl => _configuration["VoiceInput:GPTAPI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions";
        
        public int GPTTimeout => int.TryParse(_configuration["VoiceInput:GPTAPI:Timeout"], out var timeout) ? timeout : 30;
        
        public double GPTTemperature => double.TryParse(_configuration["VoiceInput:GPTAPI:Temperature"], out var temp) ? temp : 0.0;
        
        public int GPTMaxTokens => int.TryParse(_configuration["VoiceInput:GPTAPI:MaxTokens"], out var tokens) ? tokens : 1000;
        
        public int WhisperTimeout => int.TryParse(_configuration["VoiceInput:WhisperAPI:Timeout"], out var timeout) ? timeout : 30;
        
        public string WhisperLanguage => _configuration["VoiceInput:WhisperAPI:Language"] ?? "auto";
        
        public string WhisperOutputMode => _configuration["VoiceInput:WhisperAPI:OutputMode"] ?? "transcription";
        
        public double WhisperTemperature => double.TryParse(_configuration["VoiceInput:WhisperAPI:Temperature"], out var temp) ? temp : 0.0;
        
        public string WhisperResponseFormat => _configuration["VoiceInput:WhisperAPI:ResponseFormat"] ?? "json";

        public string? ApiKey 
        { 
            get => _cachedApiKey;
            set
            {
                _cachedApiKey = value;
                if (!string.IsNullOrEmpty(value))
                {
                    _secureStorage.SaveApiKey(value);
                }
                else
                {
                    _secureStorage.DeleteApiKey();
                }
            }
        }

        // 代理设置
        public bool ProxyEnabled
        {
            get => bool.TryParse(_configuration["VoiceInput:ProxySettings:Enabled"], out var result) && result;
            set => SaveSetting("VoiceInput:ProxySettings:Enabled", value.ToString());
        }

        public string ProxyAddress
        {
            get => _configuration["VoiceInput:ProxySettings:Address"] ?? "";
            set => SaveSetting("VoiceInput:ProxySettings:Address", value);
        }

        public int ProxyPort
        {
            get => int.TryParse(_configuration["VoiceInput:ProxySettings:Port"], out var port) ? port : 0;
            set => SaveSetting("VoiceInput:ProxySettings:Port", value.ToString());
        }

        public bool ProxyRequiresAuthentication
        {
            get => bool.TryParse(_configuration["VoiceInput:ProxySettings:RequiresAuthentication"], out var result) && result;
            set => SaveSetting("VoiceInput:ProxySettings:RequiresAuthentication", value.ToString());
        }

        private string? _cachedProxyUsername;
        private string? _cachedProxyPassword;

        public string? ProxyUsername
        {
            get => _cachedProxyUsername;
            set
            {
                _cachedProxyUsername = value;
                if (!string.IsNullOrEmpty(value))
                {
                    _secureStorage.SaveProxyCredentials(value, _cachedProxyPassword ?? "");
                }
                else if (string.IsNullOrEmpty(_cachedProxyPassword))
                {
                    _secureStorage.DeleteProxyCredentials();
                }
            }
        }

        public string? ProxyPassword
        {
            get => _cachedProxyPassword;
            set
            {
                _cachedProxyPassword = value;
                if (!string.IsNullOrEmpty(_cachedProxyUsername))
                {
                    _secureStorage.SaveProxyCredentials(_cachedProxyUsername, value ?? "");
                }
            }
        }

        private void LoadApiKey()
        {
            _cachedApiKey = _secureStorage.LoadApiKey();
        }

        private void LoadProxyCredentials()
        {
            var (username, password) = _secureStorage.LoadProxyCredentials();
            _cachedProxyUsername = username;
            _cachedProxyPassword = password;
        }

        public void SaveSetting(string key, string value)
        {
            try
            {
                // 获取配置文件路径
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                // 读取现有配置
                string jsonString = File.ReadAllText(configPath);
                var jsonObject = JObject.Parse(jsonString);
                
                // 分割键路径并设置值
                var keys = key.Split(':');
                JToken currentToken = jsonObject;
                
                // 导航到父节点
                for (int i = 0; i < keys.Length - 1; i++)
                {
                    if (currentToken[keys[i]] == null)
                    {
                        currentToken[keys[i]] = new JObject();
                    }
                    currentToken = currentToken[keys[i]];
                }
                
                // 设置最终值
                var lastKey = keys[keys.Length - 1];
                
                // 尝试将值转换为适当的类型
                if (bool.TryParse(value, out bool boolValue))
                {
                    currentToken[lastKey] = boolValue;
                }
                else if (int.TryParse(value, out int intValue))
                {
                    currentToken[lastKey] = intValue;
                }
                else
                {
                    currentToken[lastKey] = value;
                }
                
                // 保存回文件
                string updatedJson = jsonObject.ToString(Formatting.Indented);
                File.WriteAllText(configPath, updatedJson);
                
                LoggerService.Log($"配置已保存: {key} = {value}");
                
                // 重新加载配置
                ReloadConfiguration();
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存配置失败: {ex.Message}");
            }
        }

        // UI设置
        public string UITheme
        {
            get => _configuration["VoiceInput:UI:Theme"] ?? "Light";
            set => SaveSetting("VoiceInput:UI:Theme", value);
        }
        
        public bool ShowWaveform
        {
            get => bool.TryParse(_configuration["VoiceInput:UI:ShowWaveform"], out var result) && result;
            set => SaveSetting("VoiceInput:UI:ShowWaveform", value.ToString());
        }
        
        public int WaveformHeight
        {
            get => int.TryParse(_configuration["VoiceInput:UI:WaveformHeight"], out var height) ? height : 100;
            set => SaveSetting("VoiceInput:UI:WaveformHeight", value.ToString());
        }
        
        public string WaveformColor
        {
            get => _configuration["VoiceInput:UI:WaveformColor"] ?? "Blue";
            set => SaveSetting("VoiceInput:UI:WaveformColor", value);
        }
        
        public bool MinimizeToTray
        {
            get => bool.TryParse(_configuration["VoiceInput:UI:MinimizeToTray"], out var result) && result;
            set => SaveSetting("VoiceInput:UI:MinimizeToTray", value.ToString());
        }

        public void SaveHotkey(string hotkey)
        {
            SaveSetting("VoiceInput:Hotkey", hotkey);
        }
        
        // 批量保存方法，避免多次配置重载
        public void SaveSettingsBatch(Dictionary<string, string> settings)
        {
            try
            {
                // 获取配置文件路径
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                // 读取现有配置
                string jsonString = File.ReadAllText(configPath);
                var jsonObject = JObject.Parse(jsonString);
                
                // 批量更新所有设置
                foreach (var kvp in settings)
                {
                    var keys = kvp.Key.Split(':');
                    JToken currentToken = jsonObject;
                    
                    // 导航到父节点
                    for (int i = 0; i < keys.Length - 1; i++)
                    {
                        if (currentToken[keys[i]] == null)
                        {
                            currentToken[keys[i]] = new JObject();
                        }
                        currentToken = currentToken[keys[i]];
                    }
                    
                    // 设置最终值
                    var lastKey = keys[keys.Length - 1];
                    
                    // 尝试将值转换为适当的类型
                    if (bool.TryParse(kvp.Value, out bool boolValue))
                    {
                        currentToken[lastKey] = boolValue;
                    }
                    else if (int.TryParse(kvp.Value, out int intValue))
                    {
                        currentToken[lastKey] = intValue;
                    }
                    else
                    {
                        currentToken[lastKey] = kvp.Value;
                    }
                }
                
                // 保存回文件
                string updatedJson = jsonObject.ToString(Formatting.Indented);
                File.WriteAllText(configPath, updatedJson);
                
                LoggerService.Log($"批量保存了 {settings.Count} 个配置项");
                
                // 只重新加载一次配置
                ReloadConfiguration();
                
                // 处理特殊逻辑
                if (settings.ContainsKey("VoiceInput:AutoStart"))
                {
                    bool autoStart = bool.Parse(settings["VoiceInput:AutoStart"]);
                    _autoStartService.UpdateAutoStart(autoStart);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"批量保存配置失败: {ex.Message}");
                throw;
            }
        }
        
        public void SaveWhisperSettings(string baseUrl, int timeout, string language, string outputMode, double temperature, string? model = null)
        {
            try
            {
                // 获取配置文件路径
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                // 读取现有配置
                string jsonString = File.ReadAllText(configPath);
                var jsonObject = JObject.Parse(jsonString);
                
                // 获取WhisperAPI节点
                var whisperNode = jsonObject["VoiceInput"]["WhisperAPI"];
                if (whisperNode == null)
                {
                    jsonObject["VoiceInput"]["WhisperAPI"] = new JObject();
                    whisperNode = jsonObject["VoiceInput"]["WhisperAPI"];
                }
                
                // 批量更新所有设置
                whisperNode["BaseUrl"] = baseUrl;
                whisperNode["Timeout"] = timeout;
                whisperNode["Language"] = language;
                whisperNode["OutputMode"] = outputMode;
                whisperNode["Temperature"] = temperature;
                
                // 如果提供了模型参数，也保存模型
                if (!string.IsNullOrEmpty(model))
                {
                    whisperNode["Model"] = model;
                }
                
                // 保存回文件
                string updatedJson = jsonObject.ToString(Formatting.Indented);
                File.WriteAllText(configPath, updatedJson);
                
                LoggerService.Log($"Whisper设置已批量保存");
                
                // 只重新加载一次配置
                ReloadConfiguration();
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存Whisper设置失败: {ex.Message}");
                throw;
            }
        }
    }
}