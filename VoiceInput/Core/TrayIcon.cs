using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using VoiceInput.Services;
using VoiceInput.Views;
using Application = System.Windows.Application;

namespace VoiceInput.Core
{
    public class TrayIcon : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly AudioRecorderService _audioRecorder;
        private readonly ConfigManager _configManager;
        
        // 图标资源
        private Icon? _normalIcon;
        private Icon? _recordingIcon;

        public TrayIcon(
            GlobalHotkeyService hotkeyService, 
            AudioRecorderService audioRecorder,
            ConfigManager configManager)
        {
            _hotkeyService = hotkeyService;
            _audioRecorder = audioRecorder;
            _configManager = configManager;
        }

        public void Initialize()
        {
            // 加载图标资源
            LoadIcons();
            
            // 确保在 UI 线程中创建
            Application.Current.Dispatcher.Invoke(() =>
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = _normalIcon ?? SystemIcons.Application,
                    Visible = true,
                    Text = "语音输入法 - 按住 F3 录音"
                };

                // 双击托盘图标打开设置
                _notifyIcon.DoubleClick += (s, e) => OnSettingsClick(s, e);

                // 创建右键菜单
                var contextMenu = new ContextMenuStrip();
                
                var settingsItem = new ToolStripMenuItem("设置");
                settingsItem.Click += OnSettingsClick;
                contextMenu.Items.Add(settingsItem);
                
                contextMenu.Items.Add(new ToolStripSeparator());
                
                var exitItem = new ToolStripMenuItem("退出");
                exitItem.Click += OnExitClick;
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
                
                // 监听录音状态变化
                _audioRecorder.RecordingStateChanged += OnRecordingStateChanged;
                
                // 显示启动提示
                ShowBalloonTip("语音输入法", "已启动，按住 F3 开始录音", ToolTipIcon.Info);
            });
        }

        private void OnRecordingStateChanged(object? sender, bool isRecording)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = isRecording ? (_recordingIcon ?? SystemIcons.Information) : (_normalIcon ?? SystemIcons.Application);
                _notifyIcon.Text = isRecording ? "语音输入法 - 录音中..." : "语音输入法 - 按住 F3 录音";
            }
        }

        private void OnSettingsClick(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 重新加载配置以确保获取最新值
                _configManager.ReloadConfiguration();
                var settingsWindow = new SettingsWindow(_configManager);
                settingsWindow.ShowDialog();
            });
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadIcons()
        {
            try
            {
                // 尝试从嵌入资源加载图标
                var assembly = Assembly.GetExecutingAssembly();
                var iconResourceName = "VoiceInput.Resources.Icons.VoiceInput.ico";
                
                using (var stream = assembly.GetManifestResourceStream(iconResourceName))
                {
                    if (stream != null)
                    {
                        _normalIcon = new Icon(stream);
                        // 录音时使用相同图标，可以考虑创建不同的图标
                        _recordingIcon = new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载图标失败: {ex.Message}");
                // 如果加载失败，使用系统默认图标
                _normalIcon = SystemIcons.Application;
                _recordingIcon = SystemIcons.Information;
            }
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void Dispose()
        {
            if (_audioRecorder != null)
            {
                _audioRecorder.RecordingStateChanged -= OnRecordingStateChanged;
            }
            
            _notifyIcon?.Dispose();
            _normalIcon?.Dispose();
            _recordingIcon?.Dispose();
        }
    }
}