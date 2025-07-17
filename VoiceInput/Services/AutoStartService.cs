using System;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class AutoStartService
    {
        private const string APP_NAME = "VoiceInput";
        private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
                {
                    return key?.GetValue(APP_NAME) != null;
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"检查自动启动状态失败: {ex.Message}");
                return false;
            }
        }

        public void EnableAutoStart()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                if (exePath.EndsWith(".dll"))
                {
                    exePath = exePath.Replace(".dll", ".exe");
                }
                
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    key?.SetValue(APP_NAME, $"\"{exePath}\"");
                }
                
                LoggerService.Log("已启用开机自动启动");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"启用自动启动失败: {ex.Message}");
                throw new Exception("无法设置开机自动启动", ex);
            }
        }

        public void DisableAutoStart()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    key?.DeleteValue(APP_NAME, false);
                }
                
                LoggerService.Log("已禁用开机自动启动");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"禁用自动启动失败: {ex.Message}");
                throw new Exception("无法禁用开机自动启动", ex);
            }
        }

        public void UpdateAutoStart(bool enable)
        {
            if (enable)
            {
                EnableAutoStart();
            }
            else
            {
                DisableAutoStart();
            }
        }
    }
}