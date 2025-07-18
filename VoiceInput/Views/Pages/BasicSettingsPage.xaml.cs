using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            InitializeHotkeyInput();
        }
        
        private void InitializeHotkeyInput()
        {
            // 添加事件处理
            HotkeyBox.PreviewKeyDown += HotkeyBox_PreviewKeyDown;
            HotkeyBox.GotFocus += HotkeyBox_GotFocus;
            HotkeyBox.LostFocus += HotkeyBox_LostFocus;
            HotkeyBox.MouseDown += HotkeyBox_MouseDown;
        }
        
        private void HotkeyBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 点击时自动获取焦点
            HotkeyBox.Focus();
            e.Handled = true;
        }
        
        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            
            // 过滤一些特殊键
            if (e.Key == Key.Escape)
            {
                // Esc取消修改
                HotkeyBox.Text = _configManager.Hotkey;
                Keyboard.ClearFocus();
                return;
            }
            
            if (e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || 
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LWin || e.Key == Key.RWin)
            {
                return;
            }
            
            // 构建快捷键字符串
            string hotkeyString = "";
            
            // 检查修饰键
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                hotkeyString += "Ctrl+";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                hotkeyString += "Alt+";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                hotkeyString += "Shift+";
            
            // 添加主键
            string keyName = e.Key.ToString();
            
            // 处理功能键
            if (e.Key >= Key.F1 && e.Key <= Key.F12)
            {
                hotkeyString += keyName;
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                // 数字键
                hotkeyString += keyName.Substring(1);
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                // 字母键
                hotkeyString += keyName;
            }
            else
            {
                // 其他键
                hotkeyString += keyName;
            }
            
            // 更新显示的快捷键
            HotkeyBox.Text = hotkeyString;
        }
        
        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 获得焦点时的视觉反馈
            HotkeyBox.Background = new SolidColorBrush(Color.FromArgb(20, 0, 120, 215));
            HotkeyBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        }
        
        private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 失去焦点时恢复原样
            HotkeyBox.ClearValue(TextBox.BackgroundProperty);
            HotkeyBox.ClearValue(TextBox.BorderBrushProperty);
            
            // 如果没有有效输入，恢复原值
            if (string.IsNullOrEmpty(HotkeyBox.Text))
            {
                HotkeyBox.Text = _configManager.Hotkey;
            }
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
            // 保存 API 密钥（这个需要单独保存到凭据管理器）
            // 重要：只有在用户明确输入了新密钥时才更新，避免因为PasswordBox清空而删除已保存的密钥
            if (!string.IsNullOrEmpty(ApiKeyBox.Password))
            {
                _configManager.ApiKey = ApiKeyBox.Password;
            }

            // 批量保存其他设置，避免多次配置重载
            var settings = new System.Collections.Generic.Dictionary<string, string>
            {
                ["VoiceInput:Hotkey"] = HotkeyBox.Text,
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