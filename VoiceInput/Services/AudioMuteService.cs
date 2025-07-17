using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace VoiceInput.Services
{
    public class AudioMuteService : IDisposable
    {
        private MMDeviceEnumerator? _deviceEnumerator;
        private MMDevice? _defaultDevice;
        private readonly Dictionary<string, float> _sessionVolumes = new Dictionary<string, float>();
        private bool _isMuted;

        public AudioMuteService()
        {
            try
            {
                LoggerService.Log("音频静音服务初始化");
                _deviceEnumerator = new MMDeviceEnumerator();
                LoggerService.Log("音频静音服务初始化成功");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"音频静音服务初始化失败: {ex.Message}");
            }
        }

        public void MuteSystemAudio()
        {
            if (_isMuted)
            {
                LoggerService.Log("系统音频已经处于静音状态，跳过操作");
                return;
            }
            
            if (_deviceEnumerator == null)
            {
                LoggerService.Log("音频设备枚举器未初始化");
                return;
            }

            try
            {
                LoggerService.Log("正在静音系统音频...");
                
                // 获取默认音频设备
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                
                if (_defaultDevice != null)
                {
                    var sessionManager = _defaultDevice.AudioSessionManager;
                    if (sessionManager != null)
                    {
                        var sessions = sessionManager.Sessions;
                        LoggerService.Log($"找到 {sessions.Count} 个音频会话");
                        
                        _sessionVolumes.Clear();
                        
                        for (int i = 0; i < sessions.Count; i++)
                        {
                            try
                            {
                                var session = sessions[i];
                                var simpleVolume = session.SimpleAudioVolume;
                                
                                if (simpleVolume != null)
                                {
                                    // 保存当前音量
                                    var currentVolume = simpleVolume.Volume;
                                    var key = $"session_{i}";
                                    _sessionVolumes[key] = currentVolume;
                                    
                                    // 只有当前音量大于0时才静音，避免保存错误的音量值
                                    if (currentVolume > 0)
                                    {
                                        simpleVolume.Volume = 0.0f;
                                        LoggerService.Log($"已静音会话 {i + 1} (原音量: {currentVolume:P0})");
                                    }
                                    else
                                    {
                                        LoggerService.Log($"会话 {i + 1} 音量已经是0，跳过静音");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Log($"静音会话 {i + 1} 失败: {ex.Message}");
                            }
                        }
                    }
                }
                
                _isMuted = true;
                LoggerService.Log($"系统音频已静音（{_sessionVolumes.Count} 个会话）");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"静音系统音频失败: {ex.Message}");
            }
        }

        public void UnmuteSystemAudio()
        {
            if (!_isMuted || _defaultDevice == null) return;

            try
            {
                LoggerService.Log("正在恢复系统音频...");
                
                var sessionManager = _defaultDevice.AudioSessionManager;
                if (sessionManager != null)
                {
                    var sessions = sessionManager.Sessions;
                    
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        try
                        {
                            var session = sessions[i];
                            var key = $"session_{i}";
                            
                            var simpleVolume = session.SimpleAudioVolume;
                            if (simpleVolume != null)
                            {
                                if (_sessionVolumes.ContainsKey(key))
                                {
                                    var savedVolume = _sessionVolumes[key];
                                    // 只恢复有效的音量值
                                    if (savedVolume > 0)
                                    {
                                        simpleVolume.Volume = savedVolume;
                                        LoggerService.Log($"已恢复会话 {i + 1} 音量到 {savedVolume:P0}");
                                    }
                                    else
                                    {
                                        // 如果保存的音量是0，恢复到默认音量
                                        simpleVolume.Volume = 1.0f;
                                        LoggerService.Log($"会话 {i + 1} 保存的音量为0，恢复到100%");
                                    }
                                }
                                else
                                {
                                    // 如果没有保存的音量，检查当前音量
                                    var currentVolume = simpleVolume.Volume;
                                    if (currentVolume == 0)
                                    {
                                        // 只有当前音量是0时才恢复到默认值
                                        simpleVolume.Volume = 1.0f;
                                        LoggerService.Log($"会话 {i + 1} 没有保存的音量且当前为0，恢复到100%");
                                    }
                                    else
                                    {
                                        LoggerService.Log($"会话 {i + 1} 没有保存的音量，保持当前音量 {currentVolume:P0}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerService.Log($"恢复音频会话 {i + 1} 失败: {ex.Message}");
                        }
                    }
                }
                
                _sessionVolumes.Clear();
                _isMuted = false;
                LoggerService.Log("系统音频已恢复");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"恢复系统音频失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 强制恢复所有音频会话到100%音量
        /// 用于紧急恢复音频或修复异常情况
        /// </summary>
        public void ForceRestoreAllAudio()
        {
            try
            {
                LoggerService.Log("正在强制恢复所有音频会话...");
                
                var deviceEnumerator = new MMDeviceEnumerator();
                var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                
                if (device != null)
                {
                    var sessionManager = device.AudioSessionManager;
                    if (sessionManager != null)
                    {
                        var sessions = sessionManager.Sessions;
                        int restoredCount = 0;
                        
                        for (int i = 0; i < sessions.Count; i++)
                        {
                            try
                            {
                                var session = sessions[i];
                                var simpleVolume = session.SimpleAudioVolume;
                                
                                if (simpleVolume != null && simpleVolume.Volume == 0)
                                {
                                    simpleVolume.Volume = 1.0f;
                                    restoredCount++;
                                    LoggerService.Log($"已强制恢复会话 {i + 1} 到100%");
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Log($"强制恢复会话 {i + 1} 失败: {ex.Message}");
                            }
                        }
                        
                        LoggerService.Log($"强制恢复完成，共恢复 {restoredCount} 个静音会话");
                    }
                }
                
                // 清理状态
                _sessionVolumes.Clear();
                _isMuted = false;
            }
            catch (Exception ex)
            {
                LoggerService.Log($"强制恢复音频失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // 确保恢复音频
            if (_isMuted)
            {
                UnmuteSystemAudio();
            }

            _defaultDevice?.Dispose();
            _deviceEnumerator?.Dispose();
        }
    }
}