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

            LoggerService.Log("准备调用OpenAI Whisper API...");

            using var formData = new MultipartFormDataContent();
            
            // 添加音频文件
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            formData.Add(audioContent, "file", "audio.wav");
            
            // 添加模型参数
            formData.Add(new StringContent(_configManager.WhisperModel), "model");
            
            // 添加响应格式
            formData.Add(new StringContent("json"), "response_format");

            // 设置认证头
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _configManager.ApiKey);

            try
            {
                LoggerService.Log($"发送请求到 {_configManager.WhisperBaseUrl}");
                var response = await _httpClient.PostAsync(_configManager.WhisperBaseUrl, formData);
                
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
                }
                
                return result?.Text ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                LoggerService.Log($"网络请求失败: {ex.Message}");
                throw new Exception($"语音识别请求失败: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                LoggerService.Log("请求超时");
                throw new Exception($"语音识别请求超时（{_configManager.WhisperTimeout}秒）");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"未知错误: {ex.Message}");
                throw;
            }
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