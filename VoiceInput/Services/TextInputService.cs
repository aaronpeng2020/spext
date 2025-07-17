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

        public TextInputService()
        {
            _inputSimulator = new InputSimulator();
        }

        public void TypeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            LoggerService.Log($"准备输入文字: {text}");

            // 获取当前焦点窗口
            var foregroundWindow = GetForegroundWindow();
            
            if (foregroundWindow == IntPtr.Zero)
            {
                LoggerService.Log("未找到活动窗口，使用剪贴板方式");
                CopyToClipboard(text);
                return;
            }

            try
            {
                // 先尝试使用剪贴板方式（对中文支持更好）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Clipboard.SetText(text);
                });
                
                // 模拟 Ctrl+V 粘贴
                _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                
                LoggerService.Log("文字输入完成（剪贴板方式）");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"剪贴板输入失败: {ex.Message}");
                
                try
                {
                    // 如果剪贴板方式失败，尝试直接键盘输入
                    _inputSimulator.Keyboard.TextEntry(text);
                    LoggerService.Log("文字输入完成（键盘模拟方式）");
                }
                catch (Exception ex2)
                {
                    LoggerService.Log($"键盘输入也失败: {ex2.Message}");
                    throw new Exception("无法输入文字到目标窗口", ex2);
                }
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
    }
}