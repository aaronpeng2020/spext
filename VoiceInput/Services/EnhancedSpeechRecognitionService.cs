using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public interface IEnhancedSpeechRecognitionService : IDisposable
    {
        Task<string> RecognizeAsync(byte[] audioData, HotkeyProfile profile);
        Task<string> RecognizeWithDefaultAsync(byte[] audioData);
    }

    public class EnhancedSpeechRecognitionService : IEnhancedSpeechRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigManager _configManager;
        private readonly ICustomPromptService _promptService;
        private readonly ILoggerService _logger;
        private readonly SecureStorageService _secureStorage;
        private const int MaxRetries = 2;
        private const int RetryDelayMs = 1000;

        public EnhancedSpeechRecognitionService(
            ConfigManager configManager,
            ICustomPromptService promptService,
            ILoggerService logger,
            SecureStorageService secureStorage)
        {
            _configManager = configManager;
            _promptService = promptService;
            _logger = logger;
            _secureStorage = secureStorage;

            // 配置 HttpClient，包括代理设置
            var handler = new HttpClientHandler();

            if (_configManager.ProxyEnabled && !string.IsNullOrWhiteSpace(_configManager.ProxyAddress) && _configManager.ProxyPort > 0)
            {
                _logger.Info($"配置代理: {_configManager.ProxyAddress}:{_configManager.ProxyPort}");

                var proxy = new WebProxy($"http://{_configManager.ProxyAddress}:{_configManager.ProxyPort}");

                if (_configManager.ProxyRequiresAuthentication &&
                    !string.IsNullOrEmpty(_configManager.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(
                        _configManager.ProxyUsername,
                        _configManager.ProxyPassword);
                    _logger.Info("已配置代理认证");
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(_configManager.WhisperTimeout)
            };
        }

        public async Task<string> RecognizeAsync(byte[] audioData, HotkeyProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (string.IsNullOrEmpty(_configManager.ApiKey))
            {
                _logger.Error("API密钥未配置");
                throw new InvalidOperationException("API密钥未配置，请在设置中配置您的OpenAI API密钥");
            }

            // 获取处理后的转写 Prompt
            var transcriptionPrompt = !string.IsNullOrEmpty(profile.TranscriptionPrompt) 
                ? _promptService.ProcessPrompt(profile.TranscriptionPrompt, profile)
                : _promptService.ProcessPrompt(profile.CustomPrompt, profile); // 向后兼容

            // 记录详细的API调用信息
            _logger.Info($"使用配置 '{profile.Name}' 调用 Whisper API");
            _logger.Info($"输入语言: {profile.InputLanguage}");
            _logger.Info($"输出语言: {profile.OutputLanguage}");
            _logger.Info($"转写 Prompt: {transcriptionPrompt}");
            _logger.Info($"音频大小: {audioData.Length / 1024.0:F2} KB");

            using var formData = new MultipartFormDataContent();

            // 添加音频文件
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            formData.Add(audioContent, "file", "audio.wav");

            // 添加模型参数
            formData.Add(new StringContent(_configManager.WhisperModel), "model");

            // 添加响应格式
            formData.Add(new StringContent(_configManager.WhisperResponseFormat), "response_format");

            // 判断是否需要翻译
            bool needsTranslation = profile.IsTranslationEnabled;
            
            // 两阶段翻译：始终使用转写 API 获取原文
            string apiUrl = _configManager.WhisperBaseUrl;
            _logger.Info($"使用转写API: {apiUrl}");
            
            // 添加输入语言参数（如果不是自动检测）
            if (profile.InputLanguage != "auto" && profile.InputLanguage != "none")
            {
                var inputLang = GetWhisperLanguageCode(profile.InputLanguage);
                if (!string.IsNullOrEmpty(inputLang))
                {
                    formData.Add(new StringContent(inputLang), "language");
                    _logger.Info($"指定输入语言: {inputLang}");
                }
            }
            else
            {
                _logger.Info("使用自动语言检测");
            }

            // 如果不需要翻译，可以添加 Prompt 来改善转写效果
            if (!needsTranslation && !string.IsNullOrEmpty(transcriptionPrompt))
            {
                formData.Add(new StringContent(transcriptionPrompt), "prompt");
            }

            // 添加Temperature参数
            if (_configManager.WhisperTemperature != 0.0)
            {
                formData.Add(new StringContent(_configManager.WhisperTemperature.ToString()), "temperature");
            }

            // 设置认证头 - 优先使用 Whisper 专用密钥，如果没有则使用主密钥
            var whisperApiKey = _secureStorage.LoadWhisperApiKey() ?? _configManager.ApiKey;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", whisperApiKey);

            // 实现重试机制
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.Info($"重试第 {attempt} 次...");
                        await Task.Delay(RetryDelayMs * attempt);
                    }

                    _logger.Info($"发送请求到 {apiUrl}");
                    var startTime = DateTime.Now;
                    var response = await _httpClient.PostAsync(apiUrl, formData);
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    _logger.Info($"API响应时间: {elapsed:F0}ms");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.Error($"API返回错误: {response.StatusCode} - {errorContent}");

                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            throw new Exception("API密钥无效或已过期");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            if (attempt < MaxRetries)
                            {
                                _logger.Warn("API请求频率超限，准备重试");
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
                    _logger.Info($"收到API响应: {json.Length} 字符");
                    _logger.Info($"API响应内容: {json}");

                    var result = JsonConvert.DeserializeObject<WhisperResponse>(json);

                    if (result?.Text != null)
                    {
                        _logger.Info($"识别结果: {result.Text}");
                        _logger.Info($"结果长度: {result.Text.Length} 字符");

                        // 移除前后空白
                        result.Text = result.Text.Trim();

                        // 如果需要后处理（例如简繁转换）
                        result.Text = PostProcessText(result.Text, profile);
                        
                        // 如果需要翻译，使用 GPT 进行翻译
                        if (needsTranslation && !string.IsNullOrWhiteSpace(result.Text))
                        {
                            // 检测实际语言（如果是自动检测）
                            string sourceLanguage = profile.InputLanguage;
                            if (sourceLanguage == "auto")
                            {
                                // TODO: 可以通过分析文本内容来检测语言
                                // 暂时假设是中文
                                sourceLanguage = "zh-CN";
                                _logger.Info($"自动检测语言，假设为: {sourceLanguage}");
                            }
                            
                            _logger.Info($"需要翻译: {sourceLanguage} -> {profile.OutputLanguage}");
                            result.Text = await TranslateWithGPTAsync(result.Text, sourceLanguage, profile.OutputLanguage, profile);
                        }
                        
                        _logger.Info($"最终结果: {result.Text}");
                    }

                    return result?.Text ?? string.Empty;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    _logger.Error($"网络请求失败，准备重试: {ex.Message}");
                    continue;
                }
                catch (TaskCanceledException) when (attempt < MaxRetries)
                {
                    _logger.Error($"请求超时，准备重试");
                    continue;
                }
                catch (Exception ex) when (attempt < MaxRetries && !(ex.Message.Contains("API密钥")))
                {
                    _logger.Error($"请求失败，准备重试: {ex.Message}");
                    continue;
                }
            }

            // 如果所有重试都失败，抛出异常
            throw new Exception("语音识别请求失败，已达到最大重试次数");
        }

        public async Task<string> RecognizeWithDefaultAsync(byte[] audioData)
        {
            // 使用默认配置调用原始服务
            var defaultProfile = new HotkeyProfile
            {
                Name = "Default",
                InputLanguage = _configManager.WhisperLanguage ?? "auto",
                OutputLanguage = _configManager.WhisperLanguage ?? "zh-CN",
                CustomPrompt = ""
            };

            return await RecognizeAsync(audioData, defaultProfile);
        }

        // 这个方法已经不再需要，因为我们现在使用两阶段处理
        // private string DetermineApiEndpoint(HotkeyProfile profile)
        // {
        //     // 保留此方法的注释以供参考
        // }

        private string GetWhisperLanguageCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == "mixed")
            {
                return "auto";
            }

            var language = LanguageInfo.GetLanguageByCode(languageCode);
            return language?.WhisperCode ?? languageCode.Split('-')[0];
        }

        private string PostProcessText(string text, HotkeyProfile profile)
        {
            // 这里可以添加后处理逻辑，例如：
            // - 简繁转换
            // - 特殊字符处理
            // - 格式化处理

            return text;
        }

        private string GenerateTranslationPrompt(string sourceCode, string targetCode, LanguageInfo sourceLang, LanguageInfo targetLang)
        {
            var sourceName = sourceLang?.Name ?? sourceCode;
            var targetName = targetLang?.Name ?? targetCode;
            
            // 特定语言对的优化提示
            if (sourceCode == "zh-CN" && targetCode == "en-US")
            {
                return "You are a professional Chinese-English translator. Translate the Chinese text to English. " +
                       "Output ONLY the English translation. Do not include any Chinese text or explanations. " +
                       "Maintain accuracy for technical terms and proper nouns. Preserve sentence boundaries.";
            }
            else if (sourceCode == "zh-CN" && targetCode == "ja-JP")
            {
                return "You are a professional Chinese-Japanese translator. Translate the Chinese text to Japanese. " +
                       "Output ONLY the Japanese translation. Do not include any Chinese text or explanations. " +
                       "Use appropriate honorifics and formal language. Preserve sentence boundaries.";
            }
            else if (sourceCode == "zh-CN" && targetCode == "ko-KR")
            {
                return "You are a professional Chinese-Korean translator. Translate the Chinese text to Korean. " +
                       "Output ONLY the Korean translation. Do not include any Chinese text or explanations. " +
                       "Use appropriate honorifics and formal language. Preserve sentence boundaries.";
            }
            else
            {
                return $"You are a professional translator. Translate {sourceName} text to {targetName}. " +
                       "Output ONLY the translation, no explanations or original text. " +
                       "Maintain the sentence structure and formatting. Preserve all line breaks.";
            }
        }

        private async Task<string> TranslateWithGPTAsync(string text, string sourceLanguage, string targetLanguage, HotkeyProfile profile)
        {
            try
            {
                _logger.Info($"使用 GPT 翻译: {sourceLanguage} -> {targetLanguage}");
                _logger.Info($"原文: {text}");

                var sourceLang = LanguageInfo.GetLanguageByCode(sourceLanguage);
                var targetLang = LanguageInfo.GetLanguageByCode(targetLanguage);

                // 根据目标语言生成更准确的系统提示
                string systemPrompt;
                if (!string.IsNullOrEmpty(profile.TranslationPrompt))
                {
                    // 使用自定义翻译提示词
                    systemPrompt = _promptService.ProcessPrompt(profile.TranslationPrompt, profile);
                    _logger.Info($"使用自定义翻译提示词: {systemPrompt}");
                }
                else
                {
                    // 使用默认提示词
                    systemPrompt = GenerateTranslationPrompt(sourceLanguage, targetLanguage, sourceLang, targetLang);
                }

                var request = new ChatGPTRequest
                {
                    Model = _configManager.GPTModel,
                    Temperature = _configManager.GPTTemperature,
                    MaxTokens = _configManager.GPTMaxTokens,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "system", Content = systemPrompt },
                        new ChatMessage { Role = "user", Content = text }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 设置认证头 - 优先使用 GPT 专用密钥，如果没有则使用主密钥
                var gptApiKey = _secureStorage.LoadGPTApiKey() ?? _configManager.ApiKey;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", gptApiKey);

                // 设置超时
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_configManager.GPTTimeout));
                var response = await _httpClient.PostAsync(_configManager.GPTBaseUrl, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.Error($"GPT API 返回错误: {response.StatusCode} - {errorContent}");
                    throw new Exception($"GPT API 请求失败: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var gptResponse = JsonConvert.DeserializeObject<ChatGPTResponse>(responseJson);

                if (gptResponse?.Choices?.Count > 0)
                {
                    var translation = gptResponse.Choices[0].Message.Content;
                    _logger.Info($"GPT 翻译结果: {translation}");
                    return translation;
                }

                _logger.Warn("GPT API 返回空结果");
                return text;
            }
            catch (Exception ex)
            {
                _logger.Error($"GPT 翻译失败: {ex.Message}", ex);
                // 如果 GPT 翻译失败，返回原文
                return text;
            }
        }

        private class WhisperResponse
        {
            [JsonProperty("text")]
            public string Text { get; set; }
        }

        private class ChatGPTRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("messages")]
            public List<ChatMessage> Messages { get; set; }

            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("max_tokens")]
            public int MaxTokens { get; set; }
        }

        private class ChatMessage
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private class ChatGPTResponse
        {
            [JsonProperty("choices")]
            public List<ChatChoice> Choices { get; set; }
        }

        private class ChatChoice
        {
            [JsonProperty("message")]
            public ChatMessage Message { get; set; }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}