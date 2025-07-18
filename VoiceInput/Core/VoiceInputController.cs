using System;
using System.Threading.Tasks;
using VoiceInput.Services;
using VoiceInput.Views;

namespace VoiceInput.Core
{
    public class VoiceInputController
    {
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly AudioRecorderService _audioRecorder;
        private readonly SpeechRecognitionService _speechRecognition;
        private readonly TextInputService _textInput;
        private readonly TrayIcon _trayIcon;
        private readonly AudioMuteService _audioMuteService;
        private readonly ConfigManager _configManager;
        private readonly SpectrumWindow _spectrumWindow;
        private bool _isProcessing;

        public VoiceInputController(
            GlobalHotkeyService hotkeyService,
            AudioRecorderService audioRecorder,
            SpeechRecognitionService speechRecognition,
            TextInputService textInput,
            TrayIcon trayIcon,
            AudioMuteService audioMuteService,
            ConfigManager configManager)
        {
            _hotkeyService = hotkeyService;
            _audioRecorder = audioRecorder;
            _speechRecognition = speechRecognition;
            _textInput = textInput;
            _trayIcon = trayIcon;
            _audioMuteService = audioMuteService;
            _configManager = configManager;
            _spectrumWindow = new SpectrumWindow(configManager);
        }

        public void Initialize()
        {
            try
            {
                _hotkeyService.Initialize();
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;
                _audioRecorder.RecordingCompleted += OnRecordingCompleted;
                _audioRecorder.AudioDataAvailable += OnAudioDataAvailable;
                LoggerService.Log("Spext 控制器初始化完成");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"控制器初始化失败: {ex.Message}");
                _trayIcon.ShowBalloonTip("错误", "程序初始化失败，请检查日志", System.Windows.Forms.ToolTipIcon.Error);
                throw;
            }
        }

        private void OnHotkeyPressed(object? sender, bool isPressed)
        {
            if (_isProcessing) return;

            try
            {
                if (isPressed)
                {
                    LoggerService.Log("F3 按下 - 开始录音");
                    
                    // 如果启用了静音功能，则静音系统音频
                    if (_configManager.MuteWhileRecording)
                    {
                        _audioMuteService.MuteSystemAudio();
                    }
                    
                    _audioRecorder.StartRecording();
                    
                    // 在UI线程上调用SpectrumWindow的方法
                    _spectrumWindow.Dispatcher.Invoke(() =>
                    {
                        _spectrumWindow.StartRecording();
                    });
                }
                else
                {
                    LoggerService.Log("F3 释放 - 停止录音");
                    _audioRecorder.StopRecording();
                    
                    // 在UI线程上调用SpectrumWindow的方法
                    _spectrumWindow.Dispatcher.Invoke(() =>
                    {
                        _spectrumWindow.StopRecording();
                    });
                    
                    // 恢复系统音频
                    if (_configManager.MuteWhileRecording)
                    {
                        _audioMuteService.UnmuteSystemAudio();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"热键处理异常: {ex.Message}");
            }
        }

        private async void OnRecordingCompleted(object? sender, byte[] audioData)
        {
            if (_isProcessing || audioData.Length == 0)
            {
                if (audioData.Length == 0)
                {
                    LoggerService.Log("录音数据为空，跳过处理");
                }
                return;
            }

            _isProcessing = true;
            LoggerService.Log($"录音完成，音频大小: {audioData.Length / 1024}KB");

            try
            {
                LoggerService.Log("开始调用语音识别API...");
                // 移除识别中的提示
                // _trayIcon.ShowBalloonTip("语音输入", "正在识别...", System.Windows.Forms.ToolTipIcon.Info);

                var text = await _speechRecognition.RecognizeAsync(audioData);

                if (!string.IsNullOrEmpty(text))
                {
                    LoggerService.Log($"识别成功: {text}");
                    _textInput.TypeText(text);
                    // 移除识别完成的提示
                    // _trayIcon.ShowBalloonTip("语音输入", "识别完成", System.Windows.Forms.ToolTipIcon.Info);
                }
                else
                {
                    LoggerService.Log("未识别到内容");
                    // 保留警告提示，因为用户需要知道没有识别到内容
                    _trayIcon.ShowBalloonTip("Spext", "未识别到内容", System.Windows.Forms.ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"识别失败: {ex.Message}");
                // 保留错误提示，因为用户需要知道出错了
                _trayIcon.ShowBalloonTip("Spext", $"识别失败: {ex.Message}", System.Windows.Forms.ToolTipIcon.Error);
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        private void OnAudioDataAvailable(object? sender, float[] audioData)
        {
            // 将音频数据传递给频谱窗口（在UI线程上执行）
            _spectrumWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                _spectrumWindow.UpdateAudioData(audioData);
            }));
        }
        
        public void UpdateHotkey(string newHotkey)
        {
            try
            {
                LoggerService.Log($"正在更新热键为: {newHotkey}");
                _hotkeyService.UpdateHotkey(newHotkey);
            }
            catch (Exception ex)
            {
                LoggerService.Log($"更新热键失败: {ex.Message}");
                throw;
            }
        }
    }
}