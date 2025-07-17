using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VoiceInput.Services;

namespace VoiceInput.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigManager _configManager;

        public SettingsWindow(ConfigManager configManager)
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

            // 加载代理设置
            ProxyEnabledCheckBox.IsChecked = _configManager.ProxyEnabled;
            ProxyAddressBox.Text = _configManager.ProxyAddress;
            ProxyPortBox.Text = _configManager.ProxyPort > 0 ? _configManager.ProxyPort.ToString() : "";
            ProxyAuthCheckBox.IsChecked = _configManager.ProxyRequiresAuthentication;
            
            if (!string.IsNullOrEmpty(_configManager.ProxyUsername))
            {
                ProxyUsernameBox.Text = _configManager.ProxyUsername;
            }
            if (!string.IsNullOrEmpty(_configManager.ProxyPassword))
            {
                ProxyPasswordBox.Password = _configManager.ProxyPassword;
            }

            // 初始化UI状态
            UpdateProxyUIState();
            ProxyAuthCheckBox.Checked += (s, e) => ProxyAuthGrid.Visibility = Visibility.Visible;
            ProxyAuthCheckBox.Unchecked += (s, e) => ProxyAuthGrid.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存 API 密钥
                _configManager.ApiKey = ApiKeyBox.Password;

                // 保存快捷键
                _configManager.SaveHotkey(HotkeyBox.Text);

                // 保存自动启动设置
                _configManager.AutoStart = AutoStartCheckBox.IsChecked ?? false;

                // 保存录音设置
                _configManager.MuteWhileRecording = MuteWhileRecordingCheckBox.IsChecked ?? false;

                // 保存代理设置
                _configManager.ProxyEnabled = ProxyEnabledCheckBox.IsChecked ?? false;
                
                // 无论代理是否启用，都保存代理地址和端口，以便用户下次启用时不需要重新输入
                if (!string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
                {
                    _configManager.ProxyAddress = ProxyAddressBox.Text.Trim();
                }
                
                if (int.TryParse(ProxyPortBox.Text, out int port) && port > 0 && port <= 65535)
                {
                    _configManager.ProxyPort = port;
                }
                
                // 如果代理启用，验证必填项
                if (_configManager.ProxyEnabled)
                {
                    // 验证代理地址和端口
                    if (string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
                    {
                        MessageBox.Show("请输入代理地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    if (!int.TryParse(ProxyPortBox.Text, out int validPort) || validPort <= 0 || validPort > 65535)
                    {
                        MessageBox.Show("请输入有效的端口号 (1-65535)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                // 保存认证设置
                _configManager.ProxyRequiresAuthentication = ProxyAuthCheckBox.IsChecked ?? false;
                
                // 无论认证是否启用，都保存用户名和密码，以便用户下次启用时不需要重新输入
                if (!string.IsNullOrWhiteSpace(ProxyUsernameBox.Text) || !string.IsNullOrWhiteSpace(ProxyPasswordBox.Password))
                {
                    _configManager.ProxyUsername = ProxyUsernameBox.Text;
                    _configManager.ProxyPassword = ProxyPasswordBox.Password;
                }

                MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            TestResultText.Text = "正在测试...";

            try
            {
                // 保存当前的API密钥（如果用户刚输入的话）
                var testApiKey = string.IsNullOrEmpty(ApiKeyBox.Password) ? _configManager.ApiKey : ApiKeyBox.Password;
                
                if (string.IsNullOrEmpty(testApiKey))
                {
                    TestResultText.Text = "请先输入API密钥";
                    TestResultText.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // 创建一个简单的测试音频（静音）
                var testAudio = CreateSilentWav(1); // 1秒静音
                
                // 创建临时的语音识别服务进行测试
                using (var testRecognizer = new SpeechRecognitionService(_configManager))
                {
                    var result = await testRecognizer.RecognizeAsync(testAudio);
                    
                    TestResultText.Text = "测试成功！API密钥有效。";
                    TestResultText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                TestResultText.Text = $"测试失败: {ex.Message}";
                TestResultText.Foreground = System.Windows.Media.Brushes.Red;
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

        private void ProxyEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateProxyUIState();
        }

        private void UpdateProxyUIState()
        {
            bool isEnabled = ProxyEnabledCheckBox.IsChecked ?? false;
            ProxyServerGroup.IsEnabled = isEnabled;
            ProxyAuthGroup.IsEnabled = isEnabled;
            
            if (isEnabled && (ProxyAuthCheckBox.IsChecked ?? false))
            {
                ProxyAuthGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ProxyAuthGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async void TestProxyButton_Click(object sender, RoutedEventArgs e)
        {
            TestProxyButton.IsEnabled = false;
            ProxyTestResultText.Text = "正在测试代理连接...";

            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
                {
                    ProxyTestResultText.Text = "请输入代理地址";
                    ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                if (!int.TryParse(ProxyPortBox.Text, out int port) || port <= 0 || port > 65535)
                {
                    ProxyTestResultText.Text = "请输入有效的端口号";
                    ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // 创建测试请求
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var proxy = new System.Net.WebProxy($"http://{ProxyAddressBox.Text}:{port}");
                    
                    if (ProxyAuthCheckBox.IsChecked ?? false)
                    {
                        proxy.Credentials = new System.Net.NetworkCredential(
                            ProxyUsernameBox.Text, 
                            ProxyPasswordBox.Password);
                    }

                    var handler = new System.Net.Http.HttpClientHandler { Proxy = proxy };
                    using (var proxyClient = new System.Net.Http.HttpClient(handler))
                    {
                        proxyClient.Timeout = TimeSpan.FromSeconds(10);
                        
                        // 测试连接到 OpenAI API
                        var response = await proxyClient.GetAsync("https://api.openai.com/v1/models");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            ProxyTestResultText.Text = "代理连接成功！（需要API密钥验证）";
                            ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Green;
                        }
                        else if (response.IsSuccessStatusCode)
                        {
                            ProxyTestResultText.Text = "代理连接成功！";
                            ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Green;
                        }
                        else
                        {
                            ProxyTestResultText.Text = $"代理返回错误: {response.StatusCode}";
                            ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Orange;
                        }
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ProxyTestResultText.Text = $"代理连接失败: {ex.Message}";
                ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Red;
            }
            catch (TaskCanceledException)
            {
                ProxyTestResultText.Text = "代理连接超时";
                ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                ProxyTestResultText.Text = $"测试失败: {ex.Message}";
                ProxyTestResultText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                TestProxyButton.IsEnabled = true;
            }
        }
    }
}