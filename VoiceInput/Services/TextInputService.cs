using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace VoiceInput.Services
{
    public class TextInputService
    {
        private readonly InputSimulator _inputSimulator;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public TextInputService()
        {
            _inputSimulator = new InputSimulator();
        }

        /// <summary>
        /// 获取当前焦点窗口句柄
        /// </summary>
        public IntPtr GetCurrentFocusWindow()
        {
            var window = GetForegroundWindow();
            var title = GetWindowTitle(window);
            var className = GetWindowClassName(window);
            LoggerService.Log($"GetCurrentFocusWindow - 句柄: {window}, 标题: {title}, 类名: {className}");
            return window;
        }

        /// <summary>
        /// 获取当前输入焦点控件句柄
        /// </summary>
        public IntPtr GetCurrentFocusControl()
        {
            return GetFocus();
        }

        public void TypeText(string text)
        {
            TypeText(text, IntPtr.Zero);
        }

        /// <summary>
        /// 向指定窗口输入文字
        /// </summary>
        /// <param name="text">要输入的文字</param>
        /// <param name="targetWindow">目标窗口句柄，如果为IntPtr.Zero则使用当前焦点窗口</param>
        public void TypeText(string text, IntPtr targetWindow)
        {
            if (string.IsNullOrEmpty(text)) return;

            LoggerService.Log($"准备输入文字: {text}");

            // 如果指定了目标窗口，先验证窗口是否有效
            if (targetWindow != IntPtr.Zero)
            {
                if (!IsWindow(targetWindow))
                {
                    LoggerService.Log("目标窗口已关闭，使用当前焦点窗口");
                    targetWindow = IntPtr.Zero;
                }
            }

            // 获取窗口句柄
            var foregroundWindow = targetWindow != IntPtr.Zero ? targetWindow : GetForegroundWindow();
            
            if (foregroundWindow == IntPtr.Zero)
            {
                LoggerService.Log("未找到活动窗口，使用剪贴板方式");
                CopyToClipboard(text);
                return;
            }

            // 如果指定了目标窗口且与当前窗口不同，需要先激活目标窗口
            var currentWindow = GetForegroundWindow();
            bool needRestoreWindow = false;
            if (targetWindow != IntPtr.Zero && targetWindow != currentWindow)
            {
                LoggerService.Log($"切换到目标窗口");
                SetForegroundWindow(targetWindow);
                needRestoreWindow = true;
                // 给窗口一点时间来处理焦点切换
                System.Threading.Thread.Sleep(50);
            }

            // 获取窗口类名和窗口标题，用于判断应用类型
            var className = GetWindowClassName(foregroundWindow);
            // 获取目标窗口的标题，而不是当前活动窗口
            var windowTitle = GetWindowTitle(foregroundWindow);
            LoggerService.Log($"目标窗口 - 类名: {className}, 标题: {windowTitle}");

            // 根据不同的应用选择输入策略
            bool preferClipboard = false;
            
            // 微信、QQ、钉钉等IM软件通常需要使用剪贴板方式
            if (className.Contains("WeChat") || className.Contains("QQ") || 
                className.Contains("DingTalk") || className.Contains("TIM") ||
                windowTitle.Contains("微信") || windowTitle.Contains("QQ") || 
                windowTitle.Contains("钉钉") || windowTitle.Contains("TIM"))
            {
                LoggerService.Log("检测到IM软件，优先使用剪贴板方式");
                preferClipboard = true;
            }
            // Electron 应用（如 VSCode、Discord 等）
            else if (className.Contains("Chrome_WidgetWin"))
            {
                LoggerService.Log("检测到Electron应用，优先使用剪贴板方式");
                preferClipboard = true;
            }
            // Qt 应用（如 Telegram）
            else if (className.Contains("Qt") || windowTitle.Contains("Telegram"))
            {
                LoggerService.Log("检测到Qt应用，优先使用剪贴板方式");
                preferClipboard = true;
            }

            // 如果需要优先使用剪贴板，直接进入剪贴板输入流程
            if (preferClipboard)
            {
                UseClipboardInput(text);
                return;
            }

            // 其他应用按原有顺序尝试
            // 首先尝试使用 SendInput 直接输入（更可靠的方式）
            try
            {
                SendUnicodeString(text);
                LoggerService.Log("文字输入完成（SendInput Unicode方式）");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"SendInput失败: {ex.Message}，尝试使用InputSimulator");
                
                // 如果 SendInput 失败，尝试 InputSimulator
                try
                {
                    _inputSimulator.Keyboard.TextEntry(text);
                    LoggerService.Log("文字输入完成（InputSimulator方式）");
                }
                catch (Exception ex2)
                {
                    LoggerService.Log($"InputSimulator失败: {ex2.Message}，尝试使用剪贴板方式");
                    UseClipboardInput(text);
                }
            }

            // 如果切换过窗口，恢复原窗口
            if (needRestoreWindow && currentWindow != IntPtr.Zero)
            {
                SetForegroundWindow(currentWindow);
            }
        }

        private void UseClipboardInput(string text)
        {
            // 使用剪贴板方式（但要备份和恢复原内容）
            object? originalClipboardContent = null;
            bool hasOriginalContent = false;
                
            try
            {
                // 备份原剪贴板内容
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        originalClipboardContent = System.Windows.Clipboard.GetText();
                        hasOriginalContent = true;
                    }
                    else if (System.Windows.Clipboard.ContainsImage())
                    {
                        originalClipboardContent = System.Windows.Clipboard.GetImage();
                        hasOriginalContent = true;
                    }
                    else if (System.Windows.Clipboard.ContainsFileDropList())
                    {
                        originalClipboardContent = System.Windows.Clipboard.GetFileDropList();
                        hasOriginalContent = true;
                    }
                });
            }
            catch (Exception backupEx)
            {
                LoggerService.Log($"备份剪贴板内容失败: {backupEx.Message}");
            }

            // 使用重试机制设置剪贴板内容
            const int maxRetries = 3;
            const int retryDelay = 100;
            Exception? lastException = null;
            bool clipboardSetSuccess = false;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Clipboard.SetText(text);
                    });
                    
                    clipboardSetSuccess = true;
                    LoggerService.Log($"剪贴板设置成功（尝试 {i + 1}/{maxRetries}）");
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LoggerService.Log($"设置剪贴板失败（尝试 {i + 1}/{maxRetries}）: {ex.Message}");
                    
                    if (i < maxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(retryDelay);
                    }
                }
            }

            if (!clipboardSetSuccess)
            {
                LoggerService.Log($"设置剪贴板最终失败: {lastException?.Message}");
                throw new Exception("无法设置剪贴板内容，请检查是否有其他程序占用剪贴板", lastException);
            }

            try
            {
                // 稍微延迟以确保剪贴板内容已设置
                System.Threading.Thread.Sleep(50);
                
                // 模拟 Ctrl+V 粘贴
                _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                
                LoggerService.Log("文字输入完成（剪贴板方式）");
                
                // 等待一小段时间确保粘贴完成
                System.Threading.Thread.Sleep(100);
            }
            finally
            {
                // 恢复原剪贴板内容
                if (hasOriginalContent && originalClipboardContent != null)
                {
                    // 使用重试机制恢复剪贴板内容
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (originalClipboardContent is string textContent)
                                {
                                    System.Windows.Clipboard.SetText(textContent);
                                }
                                else if (originalClipboardContent is System.Windows.Media.Imaging.BitmapSource imageContent)
                                {
                                    System.Windows.Clipboard.SetImage(imageContent);
                                }
                                else if (originalClipboardContent is System.Collections.Specialized.StringCollection fileList)
                                {
                                    System.Windows.Clipboard.SetFileDropList(fileList);
                                }
                            });
                            LoggerService.Log($"已恢复原剪贴板内容（尝试 {i + 1}/{maxRetries}）");
                            break;
                        }
                        catch (Exception restoreEx)
                        {
                            LoggerService.Log($"恢复剪贴板内容失败（尝试 {i + 1}/{maxRetries}）: {restoreEx.Message}");
                            if (i < maxRetries - 1)
                            {
                                System.Threading.Thread.Sleep(retryDelay);
                            }
                        }
                    }
                }
            }
        }

        private void SendUnicodeString(string text)
        {
            var inputs = new INPUT[text.Length * 2];
            int inputIndex = 0;

            foreach (char c in text)
            {
                // Key down
                inputs[inputIndex] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = c,
                            dwFlags = KEYEVENTF_UNICODE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                inputIndex++;

                // Key up
                inputs[inputIndex] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = c,
                            dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                inputIndex++;
            }

            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result != inputs.Length)
            {
                throw new Exception($"SendInput 失败，只发送了 {result}/{inputs.Length} 个输入");
            }
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Clipboard.SetText(text);
                });
                
                LoggerService.Log("文本已复制到剪贴板");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"复制到剪贴板失败: {ex.Message}");
                throw new Exception("无法将文本复制到剪贴板", ex);
            }
        }

        public string GetActiveWindowTitle()
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);
            var handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return string.Empty;
        }

        private string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);

            if (GetWindowText(hWnd, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return string.Empty;
        }

        private string GetWindowClassName(IntPtr hWnd)
        {
            const int nChars = 256;
            var className = new StringBuilder(nChars);
            
            if (GetClassName(hWnd, className, nChars) > 0)
            {
                return className.ToString();
            }

            return string.Empty;
        }
    }
}