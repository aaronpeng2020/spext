using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoiceInput.Models
{
    public class TtsSettings : INotifyPropertyChanged
    {
        private bool _useOpenAIConfig = true;
        private string _customApiKey;
        private string _customApiUrl;
        private string _model = "tts-1";
        private double _speed = 1.0;
        private int _volume = 100;
        private Dictionary<string, string> _voiceMapping;

        public bool UseOpenAIConfig
        {
            get => _useOpenAIConfig;
            set
            {
                _useOpenAIConfig = value;
                OnPropertyChanged();
            }
        }

        public string CustomApiKey
        {
            get => _customApiKey;
            set
            {
                _customApiKey = value;
                OnPropertyChanged();
            }
        }

        public string CustomApiUrl
        {
            get => _customApiUrl;
            set
            {
                _customApiUrl = value;
                OnPropertyChanged();
            }
        }

        public string Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        public double Speed
        {
            get => _speed;
            set
            {
                _speed = Math.Max(0.5, Math.Min(2.0, value));
                OnPropertyChanged();
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Max(0, Math.Min(150, value));
                OnPropertyChanged();
            }
        }

        public Dictionary<string, string> VoiceMapping
        {
            get => _voiceMapping;
            set
            {
                _voiceMapping = value;
                OnPropertyChanged();
            }
        }

        public TtsSettings()
        {
            _voiceMapping = new Dictionary<string, string>
            {
                { "zh", "nova" },     // 中文 - 女声
                { "en", "echo" },     // 英文 - 男声
                { "ja", "alloy" },    // 日文 - 中性声
                { "default", "nova" }
            };
        }

        public string GetVoiceForLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                return _voiceMapping["default"];

            // 处理语言代码（如 zh-CN -> zh）
            var langCode = language.Split('-')[0].ToLower();

            return _voiceMapping.ContainsKey(langCode) 
                ? _voiceMapping[langCode] 
                : _voiceMapping["default"];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}