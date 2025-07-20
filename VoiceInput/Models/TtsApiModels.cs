using System.Text.Json.Serialization;

namespace VoiceInput.Models
{
    /// <summary>
    /// OpenAI TTS API 请求模型
    /// </summary>
    public class TtsApiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("input")]
        public string Input { get; set; }

        [JsonPropertyName("voice")]
        public string Voice { get; set; }

        [JsonPropertyName("response_format")]
        public string ResponseFormat { get; set; } = "mp3";

        [JsonPropertyName("speed")]
        public double Speed { get; set; } = 1.0;
    }

    /// <summary>
    /// 支持的语音类型
    /// </summary>
    public static class TtsVoices
    {
        public const string Alloy = "alloy";
        public const string Echo = "echo";
        public const string Fable = "fable";
        public const string Onyx = "onyx";
        public const string Nova = "nova";
        public const string Shimmer = "shimmer";

        public static readonly string[] AllVoices = new[]
        {
            Alloy, Echo, Fable, Onyx, Nova, Shimmer
        };

        public static string GetDisplayName(string voice)
        {
            return voice switch
            {
                Alloy => "Alloy (中性)",
                Echo => "Echo (男声)",
                Fable => "Fable (英式)",
                Onyx => "Onyx (低沉)",
                Nova => "Nova (女声)",
                Shimmer => "Shimmer (柔和)",
                _ => voice
            };
        }
    }

    /// <summary>
    /// TTS 模型类型
    /// </summary>
    public static class TtsModels
    {
        public const string Tts1 = "tts-1";
        public const string Tts1Hd = "tts-1-hd";

        public static string GetDisplayName(string model)
        {
            return model switch
            {
                Tts1 => "标准质量",
                Tts1Hd => "高质量",
                _ => model
            };
        }
    }
}