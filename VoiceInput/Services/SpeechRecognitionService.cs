using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class SpeechRecognitionService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigManager _configManager;
        private const int MaxRetries = 2;
        private const int RetryDelayMs = 1000;

        public SpeechRecognitionService(ConfigManager configManager)
        {
            _configManager = configManager;
            
            // 配置 HttpClient，包括代理设置
            var handler = new HttpClientHandler();
            
            if (_configManager.ProxyEnabled && !string.IsNullOrWhiteSpace(_configManager.ProxyAddress) && _configManager.ProxyPort > 0)
            {
                LoggerService.Log($"配置代理: {_configManager.ProxyAddress}:{_configManager.ProxyPort}");
                
                var proxy = new WebProxy($"http://{_configManager.ProxyAddress}:{_configManager.ProxyPort}");
                
                if (_configManager.ProxyRequiresAuthentication && 
                    !string.IsNullOrEmpty(_configManager.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(
                        _configManager.ProxyUsername, 
                        _configManager.ProxyPassword);
                    LoggerService.Log("已配置代理认证");
                }
                
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(_configManager.WhisperTimeout)
            };
        }

        public async Task<string> RecognizeAsync(byte[] audioData)
        {
            if (string.IsNullOrEmpty(_configManager.ApiKey))
            {
                LoggerService.Log("错误: API密钥未配置");
                throw new InvalidOperationException("API密钥未配置，请在设置中配置您的OpenAI API密钥");
            }

            // 记录详细的API调用信息
            LoggerService.Log("准备调用OpenAI Whisper API...");
            LoggerService.Log($"模型: {_configManager.WhisperModel}");
            LoggerService.Log($"语言: {_configManager.WhisperLanguage}");
            LoggerService.Log($"输出模式: {_configManager.WhisperOutputMode}");
            LoggerService.Log($"音频大小: {audioData.Length / 1024.0:F2} KB");

            using var formData = new MultipartFormDataContent();
            
            // 添加音频文件
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            formData.Add(audioContent, "file", "audio.wav");
            
            // 添加模型参数
            formData.Add(new StringContent(_configManager.WhisperModel), "model");
            
            // 添加响应格式
            formData.Add(new StringContent(_configManager.WhisperResponseFormat), "response_format");
            
            // 添加语言参数（如果不是自动检测）
            if (!string.IsNullOrEmpty(_configManager.WhisperLanguage) && _configManager.WhisperLanguage != "auto")
            {
                formData.Add(new StringContent(_configManager.WhisperLanguage), "language");
            }
            
            // 添加Temperature参数
            if (_configManager.WhisperTemperature != 0.0)
            {
                formData.Add(new StringContent(_configManager.WhisperTemperature.ToString()), "temperature");
            }

            // 设置认证头
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _configManager.ApiKey);

            // 根据输出模式选择API端点
            string apiUrl = _configManager.WhisperBaseUrl;
            if (_configManager.WhisperOutputMode == "translation")
            {
                apiUrl = apiUrl.Replace("transcriptions", "translations");
            }
            
            // 实现重试机制
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        LoggerService.Log($"重试第 {attempt} 次...");
                        await Task.Delay(RetryDelayMs * attempt);
                    }
                    
                    LoggerService.Log($"发送请求到 {apiUrl}");
                    var startTime = DateTime.Now;
                    var response = await _httpClient.PostAsync(apiUrl, formData);
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    LoggerService.Log($"API响应时间: {elapsed:F0}ms");
                
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LoggerService.Log($"API返回错误: {response.StatusCode} - {errorContent}");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            throw new Exception("API密钥无效或已过期");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            if (attempt < MaxRetries)
                            {
                                LoggerService.Log("API请求频率超限，准备重试");
                                continue;
                            }
                            throw new Exception("API请求频率超限，请稍后再试");
                        }
                        else
                        {
                            throw new Exception($"API请求失败: {response.StatusCode}");
                        }
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    LoggerService.Log("收到API响应");
                    
                    var result = JsonConvert.DeserializeObject<WhisperResponse>(json);
                    
                    if (result?.Text != null)
                    {
                        LoggerService.Log($"识别结果: {result.Text}");
                        LoggerService.Log($"结果长度: {result.Text.Length} 字符");
                        
                        // 移除前后空白
                        result.Text = result.Text.Trim();
                    }
                    
                    return result?.Text ?? string.Empty;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    LoggerService.Log($"网络请求失败，准备重试: {ex.Message}");
                    continue;
                }
                catch (TaskCanceledException) when (attempt < MaxRetries)
                {
                    LoggerService.Log($"请求超时，准备重试");
                    continue;
                }
                catch (Exception ex) when (attempt < MaxRetries && !(ex.Message.Contains("API密钥")))
                {
                    LoggerService.Log($"请求失败，准备重试: {ex.Message}");
                    continue;
                }
            }
            
            // 如果所有重试都失败，抛出异常
            throw new Exception("语音识别请求失败，已达到最大重试次数");
        }

        private class WhisperResponse
        {
            [JsonProperty("text")]
            public string? Text { get; set; }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}