using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class GlobalHotkeyService : IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WH_KEYBOARD_LL = 13;
        private const int LLKHF_INJECTED = 0x10;
        
        private readonly ConfigManager _configManager;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _keyboardProc;
        private Keys _hotkeyCode;
        private bool _isKeyPressed;
        
        public event EventHandler<bool>? HotkeyPressed; // true = pressed, false = released

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

        public GlobalHotkeyService(ConfigManager configManager)
        {
            _configManager = configManager;
        }

        public void Initialize()
        {
            // 解析快捷键
            _hotkeyCode = (Keys)Enum.Parse(typeof(Keys), _configManager.Hotkey);
            LoggerService.Log($"注册全局快捷键: {_configManager.Hotkey}");
            
            // 设置低级键盘钩子
            _keyboardProc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, 
                    GetModuleHandle(curModule.ModuleName), 0);
                
                if (_hookId == IntPtr.Zero)
                {
                    LoggerService.Log("警告: 快捷键注册失败！");
                }
                else
                {
                    LoggerService.Log("快捷键注册成功");
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                
                // 检查是否是我们的热键，并且不是注入的键盘事件
                if (kbStruct.vkCode == (int)_hotkeyCode && (kbStruct.flags & LLKHF_INJECTED) == 0)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        if (!_isKeyPressed)
                        {
                            _isKeyPressed = true;
                            
                            // 异步调用事件处理，避免阻塞钩子
                            System.Threading.Tasks.Task.Run(() => 
                            {
                                try
                                {
                                    HotkeyPressed?.Invoke(this, true);
                                }
                                catch (Exception ex)
                                {
                                    LoggerService.Log($"热键处理异常: {ex.Message}");
                                }
                            });
                        }
                        // 阻止F3键传递给其他应用程序
                        return (IntPtr)1;
                    }
                    else if (wParam == (IntPtr)WM_KEYUP)
                    {
                        if (_isKeyPressed)
                        {
                            _isKeyPressed = false;
                            
                            // 异步调用事件处理，避免阻塞钩子
                            System.Threading.Tasks.Task.Run(() => 
                            {
                                try
                                {
                                    HotkeyPressed?.Invoke(this, false);
                                }
                                catch (Exception ex)
                                {
                                    LoggerService.Log($"热键处理异常: {ex.Message}");
                                }
                            });
                        }
                        // 阻止F3键传递给其他应用程序
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }
}