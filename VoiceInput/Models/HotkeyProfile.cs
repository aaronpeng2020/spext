using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoiceInput.Models
{
    public class HotkeyProfile : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _hotkey;
        private string _inputLanguage;
        private string _outputLanguage;
        private string _customPrompt;
        private string _transcriptionPrompt;
        private string _translationPrompt;
        private string _recordingMode;
        private bool _isDefault;
        private bool _isEnabled;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                OnPropertyChanged();
            }
        }

        public string InputLanguage
        {
            get => _inputLanguage;
            set
            {
                _inputLanguage = value;
                OnPropertyChanged();
            }
        }

        public string OutputLanguage
        {
            get => _outputLanguage;
            set
            {
                _outputLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTranslationEnabled));
            }
        }

        // 是否启用翻译（当输出语言不为空且不是"auto"时）
        public bool IsTranslationEnabled => 
            !string.IsNullOrEmpty(OutputLanguage) && 
            OutputLanguage != "auto" && 
            OutputLanguage != "none";

        public string CustomPrompt
        {
            get => _customPrompt;
            set
            {
                _customPrompt = value;
                OnPropertyChanged();
            }
        }

        public string TranscriptionPrompt
        {
            get => _transcriptionPrompt;
            set
            {
                _transcriptionPrompt = value;
                OnPropertyChanged();
            }
        }

        public string TranslationPrompt
        {
            get => _translationPrompt;
            set
            {
                _translationPrompt = value;
                OnPropertyChanged();
            }
        }

        public string RecordingMode
        {
            get => _recordingMode;
            set
            {
                _recordingMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                _updatedAt = value;
                OnPropertyChanged();
            }
        }

        public HotkeyProfile()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            IsEnabled = true;
            RecordingMode = "PushToTalk"; // 默认使用按住说话模式
        }

        public HotkeyProfile Clone()
        {
            return new HotkeyProfile
            {
                Id = Id,  // 保留原始 ID，这样在编辑时可以正确验证快捷键
                Name = Name,
                Hotkey = Hotkey,
                InputLanguage = InputLanguage,
                OutputLanguage = OutputLanguage,
                CustomPrompt = CustomPrompt,
                TranscriptionPrompt = TranscriptionPrompt,
                TranslationPrompt = TranslationPrompt,
                RecordingMode = RecordingMode,
                IsDefault = IsDefault,  // 保留原始默认状态
                IsEnabled = IsEnabled,
                CreatedAt = CreatedAt,  // 保留原始创建时间
                UpdatedAt = DateTime.Now
            };
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Hotkey) &&
                   !string.IsNullOrWhiteSpace(InputLanguage) &&
                   !string.IsNullOrWhiteSpace(OutputLanguage);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}