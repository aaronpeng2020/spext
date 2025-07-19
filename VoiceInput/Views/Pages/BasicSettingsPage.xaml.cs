using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class BasicSettingsPage : Page
    {
        private readonly ConfigManager _configManager;
        private readonly SecureStorageService _secureStorage;

        public BasicSettingsPage(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            
            // 获取 SecureStorageService
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            _secureStorage = serviceProvider?.GetService<SecureStorageService>();
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载自动启动设置
            AutoStartCheckBox.IsChecked = _configManager.AutoStart;

            // 加载录音设置
            MuteWhileRecordingCheckBox.IsChecked = _configManager.MuteWhileRecording;
        }

        public void SaveSettings()
        {
            // 批量保存其他设置，避免多次配置重载
            var settings = new System.Collections.Generic.Dictionary<string, string>
            {
                ["VoiceInput:AutoStart"] = (AutoStartCheckBox.IsChecked ?? false).ToString(),
                ["VoiceInput:MuteWhileRecording"] = (MuteWhileRecordingCheckBox.IsChecked ?? false).ToString()
            };
            
            _configManager.SaveSettingsBatch(settings);
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            TestResultText.Text = "正在测试...";
            TestResultText.Foreground = (Brush)FindResource("TextSecondaryBrush");

            try
            {
                // 使用配置的API密钥进行测试，优先使用 Whisper 专用密钥
                var testApiKey = _secureStorage?.LoadWhisperApiKey() ?? _secureStorage?.LoadApiKey();
                
                if (string.IsNullOrEmpty(testApiKey))
                {
                    TestResultText.Text = "请先在转写与翻译设置中配置API密钥";
                    TestResultText.Foreground = Brushes.Red;
                    return;
                }

                // 创建一个简单的测试音频（静音）
                var testAudio = CreateSilentWav(1); // 1秒静音
                
                // 创建临时的语音识别服务进行测试
                using (var testRecognizer = new SpeechRecognitionService(_configManager))
                {
                    var result = await testRecognizer.RecognizeAsync(testAudio);
                    
                    TestResultText.Text = "测试成功！API密钥有效。";
                    TestResultText.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                TestResultText.Text = $"测试失败: {ex.Message}";
                TestResultText.Foreground = Brushes.Red;
            }
            finally
            {
                TestButton.IsEnabled = true;
            }
        }

        private byte[] CreateSilentWav(int seconds)
        {
            int sampleRate = 16000;
            int bitsPerSample = 16;
            int channels = 1;
            
            int dataSize = sampleRate * seconds * channels * (bitsPerSample / 8);
            int fileSize = dataSize + 44 - 8; // WAV header size

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(fileSize);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // chunk size
                writer.Write((short)1); // PCM format
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8)); // byte rate
                writer.Write((short)(channels * (bitsPerSample / 8))); // block align
                writer.Write((short)bitsPerSample);

                // data chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                
                // Write silence
                writer.Write(new byte[dataSize]);

                return stream.ToArray();
            }
        }
    }
}