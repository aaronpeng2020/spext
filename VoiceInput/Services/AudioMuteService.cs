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
            if (_isMuted || _deviceEnumerator == null) return;

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
                                    
                                    // 设置音量为0
                                    simpleVolume.Volume = 0.0f;
                                    LoggerService.Log($"已静音会话 {i + 1} (原音量: {currentVolume:P0})");
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
                            
                            if (_sessionVolumes.ContainsKey(key))
                            {
                                var simpleVolume = session.SimpleAudioVolume;
                                if (simpleVolume != null)
                                {
                                    simpleVolume.Volume = _sessionVolumes[key];
                                    LoggerService.Log($"已恢复会话 {i + 1} 音量到 {_sessionVolumes[key]:P0}");
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