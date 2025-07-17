using System;
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
        
        public int WhisperTimeout => int.TryParse(_configuration["VoiceInput:WhisperAPI:Timeout"], out var timeout) ? timeout : 30;

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

        private void SaveSetting(string key, string value)
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

        public void SaveHotkey(string hotkey)
        {
            SaveSetting("VoiceInput:Hotkey", hotkey);
        }
    }
}