using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public class TextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigManager _configManager;
        private readonly ILoggerService _logger;
        
        private WaveOutEvent _waveOut;
        private Mp3FileReader _audioReader;
        private CancellationTokenSource _playbackCts;
        private readonly object _playbackLock = new object();
        
        private const int MaxRetries = 1;
        private const int ApiTimeoutSeconds = 5;
        
        public bool IsSpeaking { get; private set; }
        
        public event EventHandler<string> SpeakingCompleted;
        public event EventHandler<Exception> SpeakingFailed;

        public TextToSpeechService(ConfigManager configManager, ILoggerService logger)
        {
            _configManager = configManager;
            _logger = logger;
            
            // 配置 HttpClient，包括代理设置
            var handler = new HttpClientHandler();
            
            if (_configManager.ProxyEnabled && !string.IsNullOrWhiteSpace(_configManager.ProxyAddress) && _configManager.ProxyPort > 0)
            {
                var proxy = new WebProxy($"http://{_configManager.ProxyAddress}:{_configManager.ProxyPort}");
                
                if (_configManager.ProxyRequiresAuthentication && 
                    !string.IsNullOrEmpty(_configManager.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(
                        _configManager.ProxyUsername, 
                        _configManager.ProxyPassword);
                }
                
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(ApiTimeoutSeconds)
            };
        }

        public async Task<bool> SpeakAsync(string text, string language, string voice = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Info("TTS: 文本为空，跳过朗读");
                return false;
            }

            // 如果正在朗读，先停止
            StopSpeaking();

            try
            {
                _logger.Info($"TTS: 开始朗读文本 (语言: {language}, 语音: {voice ?? "auto"})");
                
                // 预处理文本
                var processedText = PreprocessText(text);
                if (string.IsNullOrWhiteSpace(processedText))
                {
                    _logger.Info("TTS: 预处理后文本为空，跳过朗读");
                    return false;
                }

                // 获取音频流
                var audioStream = await GetTtsAudioStreamAsync(processedText, language, voice, cancellationToken);
                if (audioStream == null)
                {
                    _logger.Info("TTS: 无法获取音频流");
                    return false;
                }

                // 播放音频
                await PlayAudioStreamAsync(audioStream, cancellationToken);
                
                _logger.Info("TTS: 朗读完成");
                SpeakingCompleted?.Invoke(this, text);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.Info("TTS: 朗读被取消");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"TTS: 朗读失败 - {ex.Message}", ex);
                SpeakingFailed?.Invoke(this, ex);
                return false;
            }
            finally
            {
                IsSpeaking = false;
            }
        }

        public void StopSpeaking()
        {
            lock (_playbackLock)
            {
                _logger.Info("TTS: 停止朗读");
                
                _playbackCts?.Cancel();
                
                _waveOut?.Stop();
                _waveOut?.Dispose();
                _waveOut = null;
                
                _audioReader?.Dispose();
                _audioReader = null;
                
                IsSpeaking = false;
            }
        }

        private async Task<Stream> GetTtsAudioStreamAsync(string text, string language, string voice, CancellationToken cancellationToken)
        {
            // 获取 API 配置
            var ttsSettings = _configManager.TtsSettings;
            string apiKey;
            string apiUrl;

            if (ttsSettings.UseOpenAIConfig)
            {
                apiKey = _configManager.WhisperApiKey;
                // 从 WhisperApiUrl 提取基础 URL，然后构建 TTS 端点
                var baseUrl = _configManager.WhisperApiUrl?.Replace("/audio/transcriptions", "").TrimEnd('/');
                apiUrl = $"{baseUrl}/audio/speech";
            }
            else
            {
                apiKey = ttsSettings.CustomApiKey;
                apiUrl = ttsSettings.CustomApiUrl?.TrimEnd('/') + "/audio/speech";
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("TTS API 密钥未配置");
            }

            // 自动选择语音
            var selectedVoice = voice;
            if (string.IsNullOrEmpty(selectedVoice) || selectedVoice == "auto")
            {
                selectedVoice = ttsSettings.GetVoiceForLanguage(language);
            }

            // 构建请求
            var request = new TtsApiRequest
            {
                Model = ttsSettings.Model,
                Input = text,
                Voice = selectedVoice,
                ResponseFormat = "mp3",
                Speed = ttsSettings.Speed
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 重试逻辑
            for (int retry = 0; retry <= MaxRetries; retry++)
            {
                try
                {
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                    {
                        Content = content,
                        Headers = { Authorization = new AuthenticationHeaderValue("Bearer", apiKey) }
                    };

                    var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var memoryStream = new MemoryStream();
                        await response.Content.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        return memoryStream;
                    }

                    var error = await response.Content.ReadAsStringAsync();
                    _logger.Error($"TTS API 错误 ({response.StatusCode}): {error}");

                    if (retry < MaxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error($"TTS API 请求失败 (尝试 {retry + 1}/{MaxRetries + 1}): {ex.Message}", ex);
                    
                    if (retry < MaxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return null;
        }

        private async Task PlayAudioStreamAsync(Stream audioStream, CancellationToken cancellationToken)
        {
            lock (_playbackLock)
            {
                IsSpeaking = true;
                _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            try
            {
                // 使用 NAudio 播放 MP3 流
                _audioReader = new Mp3FileReader(audioStream);
                
                // 获取音量设置
                var volumePercent = _configManager.TtsSettings.Volume;
                var volumeMultiplier = volumePercent / 100f;
                
                // 如果音量超过100%，使用音频采样放大
                if (volumeMultiplier > 1.0f)
                {
                    // 转换为采样提供器并应用音量放大
                    var sampleProvider = _audioReader.ToSampleProvider();
                    var volumeProvider = new VolumeSampleProvider(sampleProvider)
                    {
                        Volume = volumeMultiplier
                    };
                    
                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(volumeProvider);
                    _waveOut.Volume = 1.0f; // 硬件音量设为最大
                }
                else
                {
                    // 音量在100%以内，直接使用硬件音量控制
                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(_audioReader);
                    _waveOut.Volume = volumeMultiplier;
                }
                
                // 播放完成事件
                var playbackFinished = new TaskCompletionSource<bool>();
                _waveOut.PlaybackStopped += (s, e) => playbackFinished.TrySetResult(true);
                
                _waveOut.Play();
                
                // 等待播放完成或取消
                using (_playbackCts.Token.Register(() => playbackFinished.TrySetCanceled()))
                {
                    await playbackFinished.Task;
                }
            }
            finally
            {
                audioStream?.Dispose();
            }
        }

        private string PreprocessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // 移除多余的标点符号
            text = Regex.Replace(text, @"[。，！？]{2,}", m => m.Value[0].ToString());
            
            // 移除特殊字符（保留基本标点和常用文字）
            text = Regex.Replace(text, @"[^\w\s\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff，。！？,.!?:：；;""'''\-]", " ");
            
            // 合并多个空格
            text = Regex.Replace(text, @"\s+", " ");
            
            // 修剪空白
            return text.Trim();
        }

        public void Dispose()
        {
            StopSpeaking();
            _httpClient?.Dispose();
        }
    }
}