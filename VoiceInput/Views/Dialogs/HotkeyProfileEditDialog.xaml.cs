using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using VoiceInput.Models;
using VoiceInput.Services;

namespace VoiceInput.Views.Dialogs
{
    public partial class HotkeyProfileEditDialog : Window
    {
        private readonly IHotkeyProfileService _profileService;
        private readonly ILoggerService _logger;
        private readonly ICustomPromptService _promptService;
        private bool _isRecording;
        private IntPtr _hookId = IntPtr.Zero;

        public HotkeyProfile Profile { get; set; }
        public string DialogTitle => Profile.Id == null ? "新建快捷键配置" : "编辑快捷键配置";
        
        public bool IsPushToTalkMode
        {
            get => Profile?.RecordingMode != "Toggle";
            set
            {
                if (value && Profile != null)
                    Profile.RecordingMode = "PushToTalk";
            }
        }
        
        public bool IsToggleMode
        {
            get => Profile?.RecordingMode == "Toggle";
            set
            {
                if (value && Profile != null)
                    Profile.RecordingMode = "Toggle";
            }
        }

        // Windows API
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public HotkeyProfileEditDialog(IHotkeyProfileService profileService, ILoggerService logger, HotkeyProfile profile = null)
        {
            _profileService = profileService;
            _logger = logger;
            
            var app = System.Windows.Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            _promptService = serviceProvider?.GetService<ICustomPromptService>();

            Profile = profile ?? new HotkeyProfile
            {
                Name = "新配置",
                Hotkey = "F4",
                InputLanguage = "zh-CN",
                OutputLanguage = "zh-CN",
                IsEnabled = true
            };

            InitializeComponent();
            DataContext = this;
            
            LoadLanguages();
            LoadTranslationTemplates();
            
            // 设置默认 Prompt
            if (string.IsNullOrEmpty(Profile.CustomPrompt) && _promptService != null)
            {
                Profile.CustomPrompt = _promptService.GetDefaultPrompt(Profile.InputLanguage, Profile.OutputLanguage);
            }
            
            // 初始化字符计数
            Loaded += (s, e) =>
            {
                TranscriptionPromptTextBox_TextChanged(null, null);
                TranslationPromptTextBox_TextChanged(null, null);
            };
        }

        private void LoadLanguages()
        {
            var languages = LanguageInfo.GetSupportedLanguages();
            
            // 输入语言：添加"自动检测"选项
            InputLanguageComboBox.Items.Add(new LanguageInfo("auto", "Auto Detect", "自动检测"));
            foreach (var language in languages.Where(l => l.Code != "mixed"))
            {
                InputLanguageComboBox.Items.Add(language);
            }
            
            // 输出语言：添加"不翻译"选项
            OutputLanguageComboBox.Items.Add(new LanguageInfo("none", "No Translation", "不翻译"));
            foreach (var language in languages.Where(l => l.Code != "mixed"))
            {
                OutputLanguageComboBox.Items.Add(language);
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            RecordButton.Content = "停止";
            HotkeyTextBox.Text = "请按下快捷键...";
            
            // 设置键盘钩子
            _proc = HookCallback;
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void StopRecording()
        {
            _isRecording = false;
            RecordButton.Content = "录制";
            
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;
                
                // 只接受功能键
                if (key >= Keys.F1 && key <= Keys.F12)
                {
                    Profile.Hotkey = key.ToString();
                    HotkeyTextBox.Text = Profile.Hotkey;
                    
                    // 在UI线程上停止录制
                    Dispatcher.BeginInvoke(new Action(() => StopRecording()));
                    
                    return (IntPtr)1; // 阻止按键传递
                }
            }
            
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            // 验证输入
            if (string.IsNullOrWhiteSpace(Profile.Name))
            {
                ShowError("请输入配置名称");
                return;
            }

            if (string.IsNullOrWhiteSpace(Profile.Hotkey))
            {
                ShowError("请设置快捷键");
                return;
            }

            // 检查快捷键冲突
            if (!await _profileService.ValidateHotkeyAsync(Profile.Hotkey, Profile.Id))
            {
                ShowError($"快捷键 {Profile.Hotkey} 已被其他配置使用");
                return;
            }

            // 更新 Prompt（如果语言设置改变了）
            if (_promptService != null && string.IsNullOrEmpty(Profile.CustomPrompt))
            {
                Profile.CustomPrompt = _promptService.GetDefaultPrompt(Profile.InputLanguage, Profile.OutputLanguage);
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecording)
            {
                StopRecording();
            }
            
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_isRecording)
            {
                StopRecording();
            }
            
            base.OnClosed(e);
        }

        private void LoadTranslationTemplates()
        {
            if (_promptService == null) return;
            
            TranslationTemplateComboBox.Items.Clear();
            
            // 添加"自定义"选项
            TranslationTemplateComboBox.Items.Add(new { Name = "自定义", Template = "" });
            
            // 获取并添加模板
            var templates = _promptService.GetTranslationTemplates(Profile.InputLanguage, Profile.OutputLanguage);
            foreach (var template in templates)
            {
                TranslationTemplateComboBox.Items.Add(template);
            }
            
            // 设置默认选择
            TranslationTemplateComboBox.SelectedIndex = 0;
            
            // 如果没有翻译提示词，设置默认
            if (string.IsNullOrEmpty(Profile.TranslationPrompt) && Profile.IsTranslationEnabled)
            {
                Profile.TranslationPrompt = _promptService.GetDefaultTranslationPrompt(Profile.InputLanguage, Profile.OutputLanguage);
            }
        }

        private void TranslationTemplateComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TranslationTemplateComboBox.SelectedItem == null) return;
            
            var selected = TranslationTemplateComboBox.SelectedItem;
            if (selected is TranslationPromptTemplates.PromptTemplate template)
            {
                TranslationPromptTextBox.Text = template.Template;
            }
        }

        private void InputLanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadTranslationTemplates();
        }

        private void OutputLanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadTranslationTemplates();
        }

        private void TranscriptionPromptTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TranscriptionCharCountText != null && TranscriptionPromptTextBox != null)
            {
                var length = TranscriptionPromptTextBox.Text?.Length ?? 0;
                TranscriptionCharCountText.Text = $"{length}/2000";
                
                // 当接近限制时改变颜色
                if (length > 1800)
                {
                    TranscriptionCharCountText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (length > 1500)
                {
                    TranscriptionCharCountText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    TranscriptionCharCountText.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }

        private void TranslationPromptTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TranslationCharCountText != null && TranslationPromptTextBox != null)
            {
                var length = TranslationPromptTextBox.Text?.Length ?? 0;
                TranslationCharCountText.Text = $"{length}/2000";
                
                // 当接近限制时改变颜色
                if (length > 1800)
                {
                    TranslationCharCountText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (length > 1500)
                {
                    TranslationCharCountText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    TranslationCharCountText.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }
    }
}