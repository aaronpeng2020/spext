using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class BasicSettingsPage : Page
    {
        private readonly ConfigManager _configManager;

        public BasicSettingsPage(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载 API 密钥
            if (!string.IsNullOrEmpty(_configManager.ApiKey))
            {
                ApiKeyBox.Password = _configManager.ApiKey;
            }

            // 加载快捷键
            HotkeyBox.Text = _configManager.Hotkey;

            // 加载自动启动设置
            AutoStartCheckBox.IsChecked = _configManager.AutoStart;

            // 加载录音设置
            MuteWhileRecordingCheckBox.IsChecked = _configManager.MuteWhileRecording;
        }

        public void SaveSettings()
        {
            // 保存 API 密钥
            _configManager.ApiKey = ApiKeyBox.Password;

            // 保存快捷键
            _configManager.SaveHotkey(HotkeyBox.Text);

            // 保存自动启动设置
            _configManager.AutoStart = AutoStartCheckBox.IsChecked ?? false;

            // 保存录音设置
            _configManager.MuteWhileRecording = MuteWhileRecordingCheckBox.IsChecked ?? false;
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            TestResultText.Text = "正在测试...";
            TestResultText.Foreground = (Brush)FindResource("TextSecondaryBrush");

            try
            {
                // 保存当前的API密钥（如果用户刚输入的话）
                var testApiKey = string.IsNullOrEmpty(ApiKeyBox.Password) ? _configManager.ApiKey : ApiKeyBox.Password;
                
                if (string.IsNullOrEmpty(testApiKey))
                {
                    TestResultText.Text = "请先输入API密钥";
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