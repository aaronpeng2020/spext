using System.Collections.Generic;

namespace VoiceInput.Services
{
    public static class TranslationPromptTemplates
    {
        public static Dictionary<string, List<PromptTemplate>> GetTranslationTemplates()
        {
            return new Dictionary<string, List<PromptTemplate>>
            {
                // 中文到英文
                ["zh-CN->en-US"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "专业翻译",
                        Template = "You are a professional Chinese-English translator. Translate the Chinese text to English. Output ONLY the English translation without any Chinese text or explanations. Maintain accuracy for technical terms and proper nouns."
                    },
                    new PromptTemplate
                    {
                        Name = "商务翻译",
                        Template = "You are a business translator. Translate the Chinese text to professional business English. Output ONLY the translation. Use formal business terminology and maintain a professional tone."
                    },
                    new PromptTemplate
                    {
                        Name = "技术文档翻译",
                        Template = "You are a technical documentation translator. Translate the Chinese technical content to English. Output ONLY the translation. Preserve technical terms, code snippets, and maintain precise technical accuracy."
                    }
                },
                
                // 中文到日文
                ["zh-CN->ja-JP"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "标准翻译",
                        Template = "You are a professional Chinese-Japanese translator. Translate the Chinese text to Japanese. Output ONLY the Japanese translation. Use appropriate honorifics and formal language."
                    },
                    new PromptTemplate
                    {
                        Name = "商务日语",
                        Template = "You are a business Japanese translator. Translate to formal business Japanese (敬語). Output ONLY the translation. Use appropriate keigo and business expressions."
                    }
                },
                
                // 英文到中文
                ["en-US->zh-CN"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "标准翻译",
                        Template = "你是一位专业的英中翻译。将英文翻译成简体中文。只输出中文翻译结果，不要包含任何英文或解释。保持专业术语的准确性。"
                    },
                    new PromptTemplate
                    {
                        Name = "技术翻译",
                        Template = "你是技术文档翻译专家。将英文技术内容翻译成简体中文。只输出中文翻译。保留必要的英文技术术语，用中文解释其含义。"
                    },
                    new PromptTemplate
                    {
                        Name = "口语化翻译",
                        Template = "将英文翻译成通俗易懂的简体中文。只输出中文翻译。使用日常用语，避免过于书面化的表达。"
                    }
                },
                
                // 日文到中文
                ["ja-JP->zh-CN"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "标准翻译",
                        Template = "你是专业的日中翻译。将日文翻译成简体中文。只输出中文翻译结果。准确传达原文含义，注意敬语的恰当转换。"
                    }
                },
                
                // 中文到中文（简繁转换或润色）
                ["zh-CN->zh-CN"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "语音识别校正（推荐）",
                        Template = @"---

### ✅ 基础处理规则

* rule: This is the text result from user speech recognition.
* rule: Please correct any misspellings, homophone errors, and basic grammar mistakes.
* rule: Add appropriate punctuation to ensure the output is readable and natural.
* rule: Only output the corrected text — no explanations, no quotes, and no additional commentary.

---

### ✅ 中英文混合处理规则

* rule: Users may mix Chinese and English words in their input.
* rule: Maintain this mixed-language format in the output.
* rule: Do not translate embedded English words (e.g., ""budget"", ""meeting"", ""review"") into Chinese.
* rule: Preserve such embedded English words exactly as given, unless they contain spelling errors.
* rule: If an embedded English word has a spelling error (e.g., ""budegt""), correct it to the proper form (""budget"") without translating it.
* rule: All other (non-embedded) content should be in the specified target language: {output_language}.
* rule: Ensure the overall sentence is grammatically correct, with proper punctuation and spacing between Chinese and English segments where needed.

---

### ✅ 短文本处理规则

* rule: If the user input is very short (e.g., one or two words or characters), do not attempt to complete or extend the sentence.
* rule: For example, if the input is ""你好"", do not change it to ""你好，有什么可以帮到你""。
* rule: Do not interpret short phrases or greetings as conversation starters — treat them as text to correct only.

---

### ✅ 输入视角规则

* rule: Treat the user input as a raw text transcription to be cleaned and corrected, not as a message requiring a reply.
* rule: Do not generate responses to the content — only return the corrected version of the original input.

---

### ✅ 示例规则（中英混合保留示范）

* rule: Examples of **correct mixed-language preservation**:

  * Input: 请你review一下这个文档。
    Output: 请你review一下这个文档。
  * Input: 我需要整理一下这个月市场部的budget"
                    },
                    new PromptTemplate
                    {
                        Name = "文本润色",
                        Template = "请润色以下中文文本，使其更加通顺流畅。只输出润色后的中文内容。保持原意不变，改善语言表达。输出必须是简体中文。"
                    },
                    new PromptTemplate
                    {
                        Name = "语音转文字优化",
                        Template = "这是语音识别的结果，请修正其中的错别字、同音字错误，并添加适当的标点符号。只输出修正后的中文文本。确保输出是简体中文。"
                    },
                    new PromptTemplate
                    {
                        Name = "正式化",
                        Template = "将口语化的中文转换为正式书面语。只输出转换后的中文文本。保持原意，使用规范的书面表达。输出必须是简体中文。"
                    },
                    new PromptTemplate
                    {
                        Name = "简化表达",
                        Template = "将复杂的中文表达简化，使其更容易理解。只输出简化后的中文文本。保持核心意思不变。输出必须是简体中文。"
                    }
                },
                
                // 中文到繁体中文
                ["zh-CN->zh-TW"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "简繁转换",
                        Template = "將簡體中文轉換為繁體中文。只輸出繁體中文結果。注意詞彙轉換的準確性（如：軟體→軟體、信息→資訊）。"
                    }
                },
                
                // 通用模板（任意语言对）
                ["*->*"] = new List<PromptTemplate>
                {
                    new PromptTemplate
                    {
                        Name = "基础翻译",
                        Template = "Translate from {input_language} to {output_language}. Output ONLY the translation without any explanations or original text."
                    },
                    new PromptTemplate
                    {
                        Name = "保持格式",
                        Template = "Translate from {input_language} to {output_language}. Output ONLY the translation. Preserve the original formatting, line breaks, and structure."
                    },
                    new PromptTemplate
                    {
                        Name = "创意翻译",
                        Template = "Creatively translate from {input_language} to {output_language} while maintaining the original meaning and cultural context. Output ONLY the translation."
                    }
                }
            };
        }
        
        public class PromptTemplate
        {
            public string Name { get; set; }
            public string Template { get; set; }
        }
        
        /// <summary>
        /// 获取特定语言对的推荐翻译提示词
        /// </summary>
        public static string GetRecommendedPrompt(string sourceLanguage, string targetLanguage)
        {
            var templates = GetTranslationTemplates();
            var key = $"{sourceLanguage}->{targetLanguage}";
            
            if (templates.ContainsKey(key) && templates[key].Count > 0)
            {
                return templates[key][0].Template;
            }
            
            // 如果是同语言，返回特殊处理
            if (sourceLanguage == targetLanguage)
            {
                if (sourceLanguage == "zh-CN")
                {
                    // 返回语音识别校正的提示词
                    return templates["zh-CN->zh-CN"][0].Template; // 返回新的详细规则提示词
                }
            }
            
            // 返回通用模板
            return "Translate from {input_language} to {output_language}. Output ONLY the translation.";
        }
    }
}