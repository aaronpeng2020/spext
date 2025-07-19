using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public interface ICustomPromptService
    {
        string ProcessPrompt(string template, HotkeyProfile profile);
        string GetDefaultPrompt(string inputLanguage, string outputLanguage);
        List<PromptTemplate> GetPromptTemplates();
        List<TranslationPromptTemplates.PromptTemplate> GetTranslationTemplates(string sourceLanguage, string targetLanguage);
        string GetDefaultTranslationPrompt(string sourceLanguage, string targetLanguage);
        bool ValidatePrompt(string prompt, out string errorMessage);
        string SanitizePrompt(string prompt);
    }

    public class PromptTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Template { get; set; }
        public string Category { get; set; }
        public List<string> SupportedLanguages { get; set; }

        public PromptTemplate()
        {
            SupportedLanguages = new List<string>();
        }
    }

    public class CustomPromptService : ICustomPromptService
    {
        private readonly ILoggerService _logger;
        private readonly Dictionary<string, string> _promptVariables;
        private readonly List<PromptTemplate> _templates;
        private const int MaxPromptLength = 2000;  // 提高到 2000 字符，足够复杂的翻译提示词使用

        public CustomPromptService(ILoggerService logger)
        {
            _logger = logger;
            _promptVariables = InitializePromptVariables();
            _templates = InitializePromptTemplates();
        }

        public string ProcessPrompt(string template, HotkeyProfile profile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(template))
                {
                    return GetDefaultPrompt(profile.InputLanguage, profile.OutputLanguage);
                }

                var processedPrompt = template;

                // 替换变量
                processedPrompt = ReplaceVariables(processedPrompt, profile);

                // 清理和验证
                processedPrompt = SanitizePrompt(processedPrompt);

                _logger.Info($"Processed prompt for profile '{profile.Name}'");
                return processedPrompt;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to process prompt: {ex.Message}", ex);
                return GetDefaultPrompt(profile.InputLanguage, profile.OutputLanguage);
            }
        }

        public string GetDefaultPrompt(string inputLanguage, string outputLanguage)
        {
            var inputLang = LanguageInfo.GetLanguageByCode(inputLanguage);
            var outputLang = LanguageInfo.GetLanguageByCode(outputLanguage);

            if (inputLang == null || outputLang == null)
            {
                return "请将语音转换为文字。";
            }

            // 相同语言
            if (inputLanguage == outputLanguage)
            {
                if (inputLanguage == "zh-CN")
                {
                    return "请将语音转换为简体中文文字。支持中英文混合输入。";
                }
                else if (inputLanguage == "zh-TW")
                {
                    return "請將語音轉換為繁體中文文字。支持中英文混合輸入。";
                }
                else
                {
                    return $"Please convert speech to {outputLang.Name} text.";
                }
            }
            // 翻译场景 - 现在使用两阶段处理，Prompt 主要用于提高转写准确性
            else
            {
                if (inputLanguage == "zh-CN")
                {
                    return "请准确识别中文语音，包括专业术语和人名地名。支持中英文混合输入。";
                }
                else if (inputLanguage == "en-US")
                {
                    return "Accurately transcribe English speech, including technical terms and proper nouns.";
                }
                else if (inputLanguage == "ja-JP")
                {
                    return "日本語の音声を正確に認識してください。専門用語や固有名詞も含めて。";
                }
                else
                {
                    return $"Accurately transcribe {inputLang.Name} speech.";
                }
            }
        }

        public List<PromptTemplate> GetPromptTemplates()
        {
            return _templates.ToList();
        }

        public bool ValidatePrompt(string prompt, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                errorMessage = "Prompt 不能为空";
                return false;
            }

            if (prompt.Length > MaxPromptLength)
            {
                errorMessage = $"Prompt 长度不能超过 {MaxPromptLength} 个字符";
                return false;
            }

            // 检查是否包含危险字符或命令
            var dangerousPatterns = new[]
            {
                @"<script[^>]*>",
                @"javascript:",
                @"system\(",
                @"exec\(",
                @"eval\("
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase))
                {
                    errorMessage = "Prompt 包含不允许的内容";
                    return false;
                }
            }

            return true;
        }

        public string SanitizePrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return string.Empty;

            // 移除多余的空白字符
            prompt = Regex.Replace(prompt, @"\s+", " ").Trim();

            // 限制长度
            if (prompt.Length > MaxPromptLength)
            {
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            // 移除特殊控制字符
            prompt = Regex.Replace(prompt, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            return prompt;
        }

        public List<TranslationPromptTemplates.PromptTemplate> GetTranslationTemplates(string sourceLanguage, string targetLanguage)
        {
            var allTemplates = TranslationPromptTemplates.GetTranslationTemplates();
            var key = $"{sourceLanguage}->{targetLanguage}";
            
            var templates = new List<TranslationPromptTemplates.PromptTemplate>();
            
            // 添加特定语言对的模板
            if (allTemplates.ContainsKey(key))
            {
                templates.AddRange(allTemplates[key]);
            }
            
            // 添加通用模板
            if (allTemplates.ContainsKey("*->*"))
            {
                templates.AddRange(allTemplates["*->*"]);
            }
            
            return templates;
        }

        public string GetDefaultTranslationPrompt(string sourceLanguage, string targetLanguage)
        {
            return TranslationPromptTemplates.GetRecommendedPrompt(sourceLanguage, targetLanguage);
        }

        private string ReplaceVariables(string template, HotkeyProfile profile)
        {
            var result = template;

            // 替换基本变量
            var replacements = new Dictionary<string, string>
            {
                { "{input_language}", GetLanguageDisplayName(profile.InputLanguage) },
                { "{output_language}", GetLanguageDisplayName(profile.OutputLanguage) },
                { "{input_language_code}", profile.InputLanguage },
                { "{output_language_code}", profile.OutputLanguage },
                { "{profile_name}", profile.Name },
                { "{date}", DateTime.Now.ToString("yyyy-MM-dd") },
                { "{time}", DateTime.Now.ToString("HH:mm:ss") }
            };

            foreach (var replacement in replacements)
            {
                result = result.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        private string GetLanguageDisplayName(string languageCode)
        {
            var language = LanguageInfo.GetLanguageByCode(languageCode);
            return language?.NativeName ?? languageCode;
        }

        private Dictionary<string, string> InitializePromptVariables()
        {
            return new Dictionary<string, string>
            {
                { "{input_language}", "输入语言名称" },
                { "{output_language}", "输出语言名称" },
                { "{input_language_code}", "输入语言代码" },
                { "{output_language_code}", "输出语言代码" },
                { "{profile_name}", "配置名称" },
                { "{date}", "当前日期" },
                { "{time}", "当前时间" }
            };
        }

        private List<PromptTemplate> InitializePromptTemplates()
        {
            return new List<PromptTemplate>
            {
                new PromptTemplate
                {
                    Id = "simple-transcription",
                    Name = "简单转写",
                    Description = "将语音转换为相同语言的文字",
                    Template = "请将语音转换为{output_language}文字。",
                    Category = "转写",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "mixed-input",
                    Name = "混合输入",
                    Description = "支持中英文混合输入",
                    Template = "请将语音转换为{output_language}文字。支持中英文混合输入。",
                    Category = "转写",
                    SupportedLanguages = new List<string> { "zh-CN", "zh-TW" }
                },
                new PromptTemplate
                {
                    Id = "technical-terms",
                    Name = "技术术语识别",
                    Description = "提高技术术语的识别准确性",
                    Template = "请准确识别{input_language}语音，特别注意技术术语、专业词汇和英文缩写。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "names-places",
                    Name = "人名地名识别",
                    Description = "提高人名和地名的识别准确性",
                    Template = "请准确识别{input_language}语音，特别注意人名、地名和机构名称。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "mixed-language",
                    Name = "混合语言识别",
                    Description = "支持多语言混合输入",
                    Template = "请准确识别语音内容，支持{input_language}和英文混合输入，保持原始语言不变。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "zh-CN", "zh-TW", "ja-JP", "ko-KR" }
                },
                new PromptTemplate
                {
                    Id = "punctuation-full",
                    Name = "完整标点",
                    Description = "添加完整的标点符号",
                    Template = "请将语音转换为{output_language}文字。请添加适当的标点符号，包括句号、逗号等。",
                    Category = "格式",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "punctuation-minimal",
                    Name = "最少标点",
                    Description = "只添加必要的标点",
                    Template = "请将语音转换为{output_language}文字。只添加必要的标点符号。",
                    Category = "格式",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "code-comments",
                    Name = "代码注释",
                    Description = "适合输入代码注释",
                    Template = "请将语音转换为{output_language}的代码注释。保持技术术语的准确性。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "medical-transcription",
                    Name = "医疗记录",
                    Description = "医疗领域专业转写",
                    Template = "请将语音转换为{output_language}医疗记录。使用标准医学术语。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "all" }
                },
                new PromptTemplate
                {
                    Id = "legal-transcription",
                    Name = "法律文书",
                    Description = "法律领域专业转写",
                    Template = "请将语音转换为{output_language}法律文书。使用规范的法律术语。",
                    Category = "专业",
                    SupportedLanguages = new List<string> { "all" }
                }
            };
        }
    }
}