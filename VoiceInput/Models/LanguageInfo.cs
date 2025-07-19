using System.Collections.Generic;

namespace VoiceInput.Models
{
    public class LanguageInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }
        public string WhisperCode { get; set; }

        public LanguageInfo(string code, string name, string nativeName, string whisperCode = null)
        {
            Code = code;
            Name = name;
            NativeName = nativeName;
            WhisperCode = whisperCode ?? code.Split('-')[0];
        }

        public static List<LanguageInfo> GetSupportedLanguages()
        {
            return new List<LanguageInfo>
            {
                new LanguageInfo("zh-CN", "Chinese (Simplified)", "简体中文", "zh"),
                new LanguageInfo("zh-TW", "Chinese (Traditional)", "繁體中文", "zh"),
                new LanguageInfo("en-US", "English (US)", "English", "en"),
                new LanguageInfo("en-GB", "English (UK)", "English", "en"),
                new LanguageInfo("ja-JP", "Japanese", "日本語", "ja"),
                new LanguageInfo("ko-KR", "Korean", "한국어", "ko"),
                new LanguageInfo("es-ES", "Spanish", "Español", "es"),
                new LanguageInfo("fr-FR", "French", "Français", "fr"),
                new LanguageInfo("de-DE", "German", "Deutsch", "de"),
                new LanguageInfo("ru-RU", "Russian", "Русский", "ru"),
                new LanguageInfo("pt-BR", "Portuguese (Brazil)", "Português", "pt"),
                new LanguageInfo("it-IT", "Italian", "Italiano", "it"),
                new LanguageInfo("ar-SA", "Arabic", "العربية", "ar"),
                new LanguageInfo("hi-IN", "Hindi", "हिन्दी", "hi"),
                new LanguageInfo("th-TH", "Thai", "ไทย", "th"),
                new LanguageInfo("vi-VN", "Vietnamese", "Tiếng Việt", "vi"),
                new LanguageInfo("mixed", "Mixed Languages", "混合语言", "")
            };
        }

        public static LanguageInfo GetLanguageByCode(string code)
        {
            return GetSupportedLanguages().Find(l => l.Code == code);
        }

        public override string ToString()
        {
            return $"{Name} ({NativeName})";
        }
    }
}