using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernWpf.Controls;
using VoiceInput.Services;

namespace VoiceInput.Views.Pages
{
    public partial class ProxySettingsPage : System.Windows.Controls.Page
    {
        private readonly ConfigManager _configManager;

        public ProxySettingsPage(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载代理设置
            ProxyEnabledCheckBox.IsChecked = _configManager.ProxyEnabled;
            ProxyAddressBox.Text = _configManager.ProxyAddress;
            
            if (_configManager.ProxyPort > 0)
            {
                ProxyPortBox.Value = _configManager.ProxyPort;
            }
            
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
        }

        public void SaveSettings()
        {
            // 保存代理设置
            _configManager.ProxyEnabled = ProxyEnabledCheckBox.IsChecked ?? false;
            
            // 无论代理是否启用，都保存代理地址和端口，以便用户下次启用时不需要重新输入
            if (!string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
            {
                _configManager.ProxyAddress = ProxyAddressBox.Text.Trim();
            }
            
            if (!double.IsNaN(ProxyPortBox.Value) && ProxyPortBox.Value > 0 && ProxyPortBox.Value <= 65535)
            {
                _configManager.ProxyPort = (int)ProxyPortBox.Value;
            }
            
            // 如果代理启用，验证必填项
            if (_configManager.ProxyEnabled)
            {
                // 验证代理地址和端口
                if (string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
                {
                    throw new InvalidOperationException("请输入代理地址");
                }
                
                if (double.IsNaN(ProxyPortBox.Value) || ProxyPortBox.Value <= 0 || ProxyPortBox.Value > 65535)
                {
                    throw new InvalidOperationException("请输入有效的端口号 (1-65535)");
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
        }

        private void ProxyEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateProxyUIState();
        }

        private void ProxyAuthCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ProxyAuthPanel.Visibility = (ProxyAuthCheckBox.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateProxyUIState()
        {
            bool isEnabled = ProxyEnabledCheckBox.IsChecked ?? false;
            ProxyServerCard.IsEnabled = isEnabled;
            ProxyAuthCard.IsEnabled = isEnabled;
            ProxyTestCard.IsEnabled = isEnabled;
            
            if (isEnabled && (ProxyAuthCheckBox.IsChecked ?? false))
            {
                ProxyAuthPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ProxyAuthPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void TestProxyButton_Click(object sender, RoutedEventArgs e)
        {
            TestProxyButton.IsEnabled = false;
            ProxyTestResultText.Text = "正在测试代理连接...";
            ProxyTestResultText.Foreground = (Brush)FindResource("TextSecondaryBrush");

            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(ProxyAddressBox.Text))
                {
                    ProxyTestResultText.Text = "请输入代理地址";
                    ProxyTestResultText.Foreground = Brushes.Red;
                    return;
                }
                
                if (double.IsNaN(ProxyPortBox.Value) || ProxyPortBox.Value <= 0 || ProxyPortBox.Value > 65535)
                {
                    ProxyTestResultText.Text = "请输入有效的端口号";
                    ProxyTestResultText.Foreground = Brushes.Red;
                    return;
                }

                int port = (int)ProxyPortBox.Value;

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
                            ProxyTestResultText.Foreground = Brushes.Green;
                        }
                        else if (response.IsSuccessStatusCode)
                        {
                            ProxyTestResultText.Text = "代理连接成功！";
                            ProxyTestResultText.Foreground = Brushes.Green;
                        }
                        else
                        {
                            ProxyTestResultText.Text = $"代理返回错误: {response.StatusCode}";
                            ProxyTestResultText.Foreground = Brushes.Orange;
                        }
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ProxyTestResultText.Text = $"代理连接失败: {ex.Message}";
                ProxyTestResultText.Foreground = Brushes.Red;
            }
            catch (TaskCanceledException)
            {
                ProxyTestResultText.Text = "代理连接超时";
                ProxyTestResultText.Foreground = Brushes.Red;
            }
            catch (Exception ex)
            {
                ProxyTestResultText.Text = $"测试失败: {ex.Message}";
                ProxyTestResultText.Foreground = Brushes.Red;
            }
            finally
            {
                TestProxyButton.IsEnabled = true;
            }
        }
    }
}