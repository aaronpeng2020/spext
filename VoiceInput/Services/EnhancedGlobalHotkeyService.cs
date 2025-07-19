using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public interface IEnhancedGlobalHotkeyService : IDisposable
    {
        event EventHandler<HotkeyEventArgs> HotkeyPressed;
        Task InitializeAsync();
        Task RegisterProfileHotkeysAsync();
        Task UnregisterAllHotkeysAsync();
        Task UpdateProfileHotkeyAsync(string profileId, string newHotkey);
        bool IsHotkeyRegistered(string hotkey);
    }

    public class HotkeyEventArgs : EventArgs
    {
        public HotkeyProfile Profile { get; set; }
        public bool IsPressed { get; set; }
        public string Hotkey { get; set; }

        public HotkeyEventArgs(HotkeyProfile profile, bool isPressed, string hotkey)
        {
            Profile = profile;
            IsPressed = isPressed;
            Hotkey = hotkey;
        }
    }

    public class EnhancedGlobalHotkeyService : IEnhancedGlobalHotkeyService
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WH_KEYBOARD_LL = 13;
        private const int LLKHF_INJECTED = 0x10;

        private readonly IHotkeyProfileService _profileService;
        private readonly ILoggerService _logger;
        private readonly Dictionary<Keys, HotkeyProfile> _hotkeyToProfile;
        private readonly Dictionary<Keys, bool> _keyStates;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _keyboardProc;

        public event EventHandler<HotkeyEventArgs> HotkeyPressed;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        public EnhancedGlobalHotkeyService(
            IHotkeyProfileService profileService,
            ILoggerService logger)
        {
            _profileService = profileService;
            _logger = logger;
            _hotkeyToProfile = new Dictionary<Keys, HotkeyProfile>();
            _keyStates = new Dictionary<Keys, bool>();

            // 订阅配置变更事件
            _profileService.ProfileAdded += OnProfileChanged;
            _profileService.ProfileUpdated += OnProfileChanged;
            _profileService.ProfileRemoved += OnProfileRemoved;
            _profileService.ConfigurationChanged += OnConfigurationChanged;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.Info("初始化增强型全局热键服务");

                // 设置低级键盘钩子
                _keyboardProc = HookCallback;
                using (var curProcess = Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
                        GetModuleHandle(curModule.ModuleName), 0);

                    if (_hookId == IntPtr.Zero)
                    {
                        _logger.Error("键盘钩子注册失败");
                        throw new InvalidOperationException("无法注册键盘钩子");
                    }
                }

                // 注册所有配置的热键
                await RegisterProfileHotkeysAsync();

                _logger.Info("增强型全局热键服务初始化完成");
            }
            catch (Exception ex)
            {
                _logger.Error($"初始化全局热键服务失败: {ex.Message}", ex);
                throw;
            }
        }

        public async Task RegisterProfileHotkeysAsync()
        {
            try
            {
                var profiles = await _profileService.GetEnabledProfilesAsync();

                lock (_hotkeyToProfile)
                {
                    _hotkeyToProfile.Clear();
                    _keyStates.Clear();

                    foreach (var profile in profiles)
                    {
                        if (TryParseHotkey(profile.Hotkey, out Keys key))
                        {
                            if (_hotkeyToProfile.ContainsKey(key))
                            {
                                _logger.Warn($"热键冲突: {profile.Hotkey} 已被使用");
                                continue;
                            }

                            _hotkeyToProfile[key] = profile;
                            _keyStates[key] = false;
                            _logger.Info($"注册热键: {profile.Hotkey} -> {profile.Name}");
                        }
                        else
                        {
                            _logger.Warn($"无效的热键: {profile.Hotkey}");
                        }
                    }
                }

                _logger.Info($"共注册 {_hotkeyToProfile.Count} 个热键");
            }
            catch (Exception ex)
            {
                _logger.Error($"注册热键失败: {ex.Message}", ex);
            }
        }

        public Task UnregisterAllHotkeysAsync()
        {
            lock (_hotkeyToProfile)
            {
                _hotkeyToProfile.Clear();
                _keyStates.Clear();
            }

            _logger.Info("已注销所有热键");
            return Task.CompletedTask;
        }

        public async Task UpdateProfileHotkeyAsync(string profileId, string newHotkey)
        {
            try
            {
                var profile = await _profileService.GetProfileByIdAsync(profileId);
                if (profile == null)
                {
                    _logger.Warn($"未找到配置: {profileId}");
                    return;
                }

                // 移除旧的热键
                lock (_hotkeyToProfile)
                {
                    var oldKey = _hotkeyToProfile.FirstOrDefault(kvp => kvp.Value.Id == profileId).Key;
                    if (oldKey != Keys.None)
                    {
                        _hotkeyToProfile.Remove(oldKey);
                        _keyStates.Remove(oldKey);
                    }

                    // 注册新的热键
                    if (profile.IsEnabled && TryParseHotkey(newHotkey, out Keys newKey))
                    {
                        if (!_hotkeyToProfile.ContainsKey(newKey))
                        {
                            _hotkeyToProfile[newKey] = profile;
                            _keyStates[newKey] = false;
                            _logger.Info($"更新热键: {profile.Name} -> {newHotkey}");
                        }
                        else
                        {
                            _logger.Warn($"热键冲突: {newHotkey} 已被使用");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"更新热键失败: {ex.Message}", ex);
            }
        }

        public bool IsHotkeyRegistered(string hotkey)
        {
            if (!TryParseHotkey(hotkey, out Keys key))
                return false;

            lock (_hotkeyToProfile)
            {
                return _hotkeyToProfile.ContainsKey(key);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // 检查是否是注入的键盘事件
                if ((kbStruct.flags & LLKHF_INJECTED) == 0)
                {
                    var key = (Keys)kbStruct.vkCode;

                    lock (_hotkeyToProfile)
                    {
                        if (_hotkeyToProfile.TryGetValue(key, out HotkeyProfile profile))
                        {
                            if (wParam == (IntPtr)WM_KEYDOWN)
                            {
                                if (!_keyStates[key])
                                {
                                    _keyStates[key] = true;

                                    // 异步调用事件处理
                                    Task.Run(() =>
                                    {
                                        try
                                        {
                                            var args = new HotkeyEventArgs(profile, true, profile.Hotkey);
                                            HotkeyPressed?.Invoke(this, args);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.Error($"热键按下处理异常: {ex.Message}", ex);
                                        }
                                    });
                                }
                                return (IntPtr)1; // 阻止键传递
                            }
                            else if (wParam == (IntPtr)WM_KEYUP)
                            {
                                if (_keyStates[key])
                                {
                                    _keyStates[key] = false;

                                    // 异步调用事件处理
                                    Task.Run(() =>
                                    {
                                        try
                                        {
                                            var args = new HotkeyEventArgs(profile, false, profile.Hotkey);
                                            HotkeyPressed?.Invoke(this, args);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.Error($"热键释放处理异常: {ex.Message}", ex);
                                        }
                                    });
                                }
                                return (IntPtr)1; // 阻止键传递
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool TryParseHotkey(string hotkey, out Keys key)
        {
            key = Keys.None;

            try
            {
                // 处理单键（如 F3）
                if (!hotkey.Contains("+"))
                {
                    if (Enum.TryParse<Keys>(hotkey, out key))
                    {
                        return true;
                    }
                }
                else
                {
                    // 处理组合键（如 Ctrl+F3）
                    var parts = hotkey.Split('+');
                    var mainKey = parts[parts.Length - 1];

                    // 暂时只支持单键
                    if (Enum.TryParse<Keys>(mainKey, out key))
                    {
                        _logger.Info($"组合键暂不支持，使用主键: {mainKey}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"解析快捷键失败: {ex.Message}", ex);
            }

            return false;
        }

        private void OnProfileChanged(object sender, HotkeyProfile profile)
        {
            Task.Run(async () => await RegisterProfileHotkeysAsync());
        }

        private void OnProfileRemoved(object sender, string profileId)
        {
            Task.Run(async () => await RegisterProfileHotkeysAsync());
        }

        private void OnConfigurationChanged(object sender, ProfileConfiguration config)
        {
            Task.Run(async () => await RegisterProfileHotkeysAsync());
        }

        public void Dispose()
        {
            // 取消订阅事件
            _profileService.ProfileAdded -= OnProfileChanged;
            _profileService.ProfileUpdated -= OnProfileChanged;
            _profileService.ProfileRemoved -= OnProfileRemoved;
            _profileService.ConfigurationChanged -= OnConfigurationChanged;

            // 注销钩子
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            _logger.Info("增强型全局热键服务已释放");
        }
    }
}