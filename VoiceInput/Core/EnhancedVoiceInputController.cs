using System;
using System.Threading.Tasks;
using VoiceInput.Models;
using VoiceInput.Services;
using VoiceInput.Views;

namespace VoiceInput.Core
{
    public class EnhancedVoiceInputController
    {
        private readonly IEnhancedGlobalHotkeyService _hotkeyService;
        private readonly IHotkeyProfileService _profileService;
        private readonly AudioRecorderService _audioRecorder;
        private readonly IEnhancedSpeechRecognitionService _speechRecognition;
        private readonly TextInputService _textInput;
        private readonly TrayIcon _trayIcon;
        private readonly AudioMuteService _audioMuteService;
        private readonly ConfigManager _configManager;
        private readonly ILoggerService _logger;
        private readonly SpectrumWindow _spectrumWindow;
        
        private bool _isProcessing;
        private HotkeyProfile _activeProfile;
        private IntPtr _targetWindow;
        private readonly System.Collections.Generic.Dictionary<string, bool> _recordingStates = new System.Collections.Generic.Dictionary<string, bool>();

        public EnhancedVoiceInputController(
            IEnhancedGlobalHotkeyService hotkeyService,
            IHotkeyProfileService profileService,
            AudioRecorderService audioRecorder,
            IEnhancedSpeechRecognitionService speechRecognition,
            TextInputService textInput,
            TrayIcon trayIcon,
            AudioMuteService audioMuteService,
            ConfigManager configManager,
            ILoggerService logger)
        {
            _hotkeyService = hotkeyService;
            _profileService = profileService;
            _audioRecorder = audioRecorder;
            _speechRecognition = speechRecognition;
            _textInput = textInput;
            _trayIcon = trayIcon;
            _audioMuteService = audioMuteService;
            _configManager = configManager;
            _logger = logger;
            _spectrumWindow = new SpectrumWindow(configManager);
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 初始化配置服务
                await _profileService.InitializeAsync();
                
                // 初始化热键服务
                await _hotkeyService.InitializeAsync();
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;
                
                // 设置音频录制回调
                _audioRecorder.RecordingCompleted += OnRecordingCompleted;
                _audioRecorder.AudioDataAvailable += OnAudioDataAvailable;
                
                _logger.Info("增强版 Spext 控制器初始化完成");
                
                // 显示已加载的配置
                var profiles = await _profileService.GetEnabledProfilesAsync();
                _logger.Info($"已加载 {profiles.Count} 个启用的快捷键配置");
                foreach (var profile in profiles)
                {
                    _logger.Info($"  - {profile.Hotkey}: {profile.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"控制器初始化失败: {ex.Message}", ex);
                _trayIcon.ShowBalloonTip("错误", "程序初始化失败，请检查日志", System.Windows.Forms.ToolTipIcon.Error);
                throw;
            }
        }

        private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
        {
            // 立即记录时间戳和状态
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            
            if (_isProcessing) 
            {
                _logger.Info($"[{timestamp}] 忽略热键 {e.Hotkey} - 正在处理中");
                return;
            }

            try
            {
                var recordingMode = e.Profile.RecordingMode ?? "PushToTalk";
                _logger.Info($"[{timestamp}] {e.Hotkey} {(e.IsPressed ? "按下" : "释放")} - 模式: {recordingMode}");

                if (recordingMode == "Toggle")
                {
                    // 切换模式：只在按下时处理，忽略释放事件
                    if (e.IsPressed)
                    {
                        // 获取或初始化该热键的录音状态
                        if (!_recordingStates.ContainsKey(e.Profile.Id))
                        {
                            _recordingStates[e.Profile.Id] = false;
                        }

                        var isRecording = _recordingStates[e.Profile.Id];
                        
                        if (!isRecording)
                        {
                            // 开始录音
                            _logger.Info($"[{timestamp}] {e.Hotkey} 切换模式 - 开始录音");
                            StartRecordingInternal(e.Profile);
                            _recordingStates[e.Profile.Id] = true;
                        }
                        else
                        {
                            // 停止录音
                            _logger.Info($"[{timestamp}] {e.Hotkey} 切换模式 - 停止录音");
                            StopRecordingInternal();
                            _recordingStates[e.Profile.Id] = false;
                        }
                    }
                }
                else // PushToTalk 模式（默认）
                {
                    if (e.IsPressed)
                    {
                        _logger.Info($"[{timestamp}] {e.Hotkey} 按住模式 - 开始录音");
                        StartRecordingInternal(e.Profile);
                    }
                    else
                    {
                        _logger.Info($"[{timestamp}] {e.Hotkey} 按住模式 - 停止录音");
                        StopRecordingInternal();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"热键处理异常: {ex.Message}", ex);
            }
        }

        private void StartRecordingInternal(HotkeyProfile profile)
        {
            _activeProfile = profile;
            
            // 记录当前焦点窗口
            _targetWindow = _textInput.GetCurrentFocusWindow();
            var windowTitle = _textInput.GetActiveWindowTitle();
            _logger.Info($"按键时记录焦点窗口 - 句柄: {_targetWindow}, 标题: {windowTitle}");
            
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
                // 可以在频谱窗口显示当前使用的配置
                _spectrumWindow.Title = $"录音中 - {profile.Name}";
            });
        }

        private void StopRecordingInternal()
        {
            _audioRecorder.StopRecording();
            
            // 在UI线程上调用SpectrumWindow的方法
            _spectrumWindow.Dispatcher.Invoke(() =>
            {
                _spectrumWindow.StopRecording();
                _spectrumWindow.Title = "音频频谱";
            });
            
            // 恢复系统音频
            if (_configManager.MuteWhileRecording)
            {
                _audioMuteService.UnmuteSystemAudio();
            }
        }

        private async void OnRecordingCompleted(object sender, byte[] audioData)
        {
            if (_isProcessing || audioData.Length == 0 || _activeProfile == null)
            {
                if (audioData.Length == 0)
                {
                    _logger.Warn("录音数据为空，跳过处理");
                }
                if (_activeProfile == null)
                {
                    _logger.Warn("没有活动的配置文件，跳过处理");
                }
                return;
            }

            _isProcessing = true;
            _logger.Info($"录音完成，音频大小: {audioData.Length / 1024}KB");

            try
            {
                _logger.Info($"使用配置 '{_activeProfile.Name}' 开始语音识别...");
                _logger.Info($"输入语言: {_activeProfile.InputLanguage}, 输出语言: {_activeProfile.OutputLanguage}");

                var text = await _speechRecognition.RecognizeAsync(audioData, _activeProfile);

                if (!string.IsNullOrEmpty(text))
                {
                    _logger.Info($"识别成功: {text}");
                    var currentWindow = _textInput.GetCurrentFocusWindow();
                    var currentTitle = _textInput.GetActiveWindowTitle();
                    _logger.Info($"准备输入 - 目标窗口: {_targetWindow}, 当前窗口: {currentWindow}, 当前标题: {currentTitle}");
                    _textInput.TypeText(text, _targetWindow);
                    
                    // 如果是翻译模式，可以显示更详细的提示
                    if (_activeProfile.InputLanguage != _activeProfile.OutputLanguage)
                    {
                        var inputLang = LanguageInfo.GetLanguageByCode(_activeProfile.InputLanguage)?.NativeName ?? _activeProfile.InputLanguage;
                        var outputLang = LanguageInfo.GetLanguageByCode(_activeProfile.OutputLanguage)?.NativeName ?? _activeProfile.OutputLanguage;
                        _logger.Info($"翻译完成: {inputLang} → {outputLang}");
                    }
                }
                else
                {
                    _logger.Warn("未识别到内容");
                    _trayIcon.ShowBalloonTip("Spext", "未识别到内容", System.Windows.Forms.ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"识别失败: {ex.Message}", ex);
                _trayIcon.ShowBalloonTip("Spext", $"识别失败: {ex.Message}", System.Windows.Forms.ToolTipIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                _activeProfile = null;
            }
        }
        
        private void OnAudioDataAvailable(object sender, float[] audioData)
        {
            // 将音频数据传递给频谱窗口（在UI线程上执行）
            _spectrumWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                _spectrumWindow.UpdateAudioData(audioData);
            }));
        }
        
        public async Task UpdateProfileHotkeyAsync(string profileId, string newHotkey)
        {
            try
            {
                _logger.Info($"正在更新配置 {profileId} 的热键为: {newHotkey}");
                
                // 更新配置
                var profile = await _profileService.GetProfileByIdAsync(profileId);
                if (profile != null)
                {
                    profile.Hotkey = newHotkey;
                    await _profileService.UpdateProfileAsync(profile);
                    
                    // 热键服务会自动通过事件更新
                    _logger.Info($"热键更新成功");
                }
                else
                {
                    _logger.Warn($"未找到配置: {profileId}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"更新热键失败: {ex.Message}", ex);
                throw;
            }
        }

        public async Task ReloadProfilesAsync()
        {
            try
            {
                _logger.Info("重新加载配置文件...");
                await _hotkeyService.RegisterProfileHotkeysAsync();
                
                var profiles = await _profileService.GetEnabledProfilesAsync();
                _logger.Info($"已重新加载 {profiles.Count} 个启用的快捷键配置");
            }
            catch (Exception ex)
            {
                _logger.Error($"重新加载配置失败: {ex.Message}", ex);
                throw;
            }
        }
    }
}