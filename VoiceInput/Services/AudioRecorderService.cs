using System;
using System.IO;
using NAudio.Wave;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class AudioRecorderService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private MemoryStream? _audioStream;
        private WaveFileWriter? _waveWriter;
        private readonly ConfigManager _configManager;
        
        public event EventHandler<bool>? RecordingStateChanged;
        public event EventHandler<byte[]>? RecordingCompleted;

        public bool IsRecording { get; private set; }

        public AudioRecorderService(ConfigManager configManager)
        {
            _configManager = configManager;
        }

        public void StartRecording()
        {
            if (IsRecording) return;

            try
            {
                LoggerService.Log("开始音频录制...");
                
                // 检查是否有可用的录音设备
                if (WaveInEvent.DeviceCount == 0)
                {
                    throw new InvalidOperationException("未找到可用的录音设备");
                }
                
                _audioStream = new MemoryStream();
                
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(
                        _configManager.AudioSampleRate, 
                        _configManager.AudioBitsPerSample, 
                        _configManager.AudioChannels)
                };

                _waveWriter = new WaveFileWriter(_audioStream, _waveIn.WaveFormat);
                
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.StartRecording();
                
                IsRecording = true;
                RecordingStateChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                LoggerService.Log($"录音启动失败: {ex.Message}");
                StopRecording();
                throw new Exception("无法开始录音", ex);
            }
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            IsRecording = false;
            RecordingStateChanged?.Invoke(this, false);

            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null;

            // 在关闭 WaveFileWriter 之前先刷新并获取音频数据
            byte[]? audioData = null;
            if (_waveWriter != null && _audioStream != null)
            {
                try
                {
                    _waveWriter.Flush();
                    audioData = _audioStream.ToArray();
                    LoggerService.Log($"音频录制完成，大小: {audioData.Length / 1024}KB");
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"获取音频数据失败: {ex.Message}");
                }
            }

            _waveWriter?.Dispose();
            _waveWriter = null;

            _audioStream?.Dispose();
            _audioStream = null;

            if (audioData != null && audioData.Length > 0)
            {
                RecordingCompleted?.Invoke(this, audioData);
            }
            else
            {
                LoggerService.Log("音频录制完成，但没有数据");
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public void Dispose()
        {
            StopRecording();
        }
    }
}