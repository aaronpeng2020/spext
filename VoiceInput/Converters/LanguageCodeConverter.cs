using System;
using System.Globalization;
using System.Windows.Data;
using VoiceInput.Models;

namespace VoiceInput.Converters
{
    public class LanguageCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string code)
            {
                switch (code)
                {
                    case "auto":
                        return "自动检测";
                    case "none":
                        return "不翻译";
                    case "":
                        return "未设置";
                    default:
                        var lang = LanguageInfo.GetLanguageByCode(code);
                        return lang?.NativeName ?? code;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}