using System;
using System.Collections.Generic;
using System.Linq;

namespace VoiceInput.Models
{
    public class ProfileConfiguration
    {
        public List<HotkeyProfile> Profiles { get; set; }
        public string ActiveProfileId { get; set; }
        public int Version { get; set; }
        public DateTime LastModified { get; set; }

        public ProfileConfiguration()
        {
            Profiles = new List<HotkeyProfile>();
            Version = 1;
            LastModified = DateTime.Now;
        }

        public HotkeyProfile GetProfileById(string id)
        {
            return Profiles.FirstOrDefault(p => p.Id == id);
        }

        public HotkeyProfile GetProfileByHotkey(string hotkey)
        {
            return Profiles.FirstOrDefault(p => p.Hotkey == hotkey && p.IsEnabled);
        }

        public bool HasHotkeyConflict(string hotkey, string excludeId = null)
        {
            return Profiles.Any(p => p.Hotkey == hotkey && p.Id != excludeId && p.IsEnabled);
        }

        public void AddProfile(HotkeyProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (!profile.IsValid())
                throw new ArgumentException("Profile is not valid");

            if (HasHotkeyConflict(profile.Hotkey, profile.Id))
                throw new InvalidOperationException($"Hotkey '{profile.Hotkey}' is already in use");

            Profiles.Add(profile);
            LastModified = DateTime.Now;
        }

        public void UpdateProfile(HotkeyProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var existingProfile = GetProfileById(profile.Id);
            if (existingProfile == null)
                throw new InvalidOperationException($"Profile with ID '{profile.Id}' not found");

            if (!profile.IsValid())
                throw new ArgumentException("Profile is not valid");

            if (HasHotkeyConflict(profile.Hotkey, profile.Id))
                throw new InvalidOperationException($"Hotkey '{profile.Hotkey}' is already in use");

            var index = Profiles.IndexOf(existingProfile);
            Profiles[index] = profile;
            profile.UpdatedAt = DateTime.Now;
            LastModified = DateTime.Now;
        }

        public void RemoveProfile(string profileId)
        {
            var profile = GetProfileById(profileId);
            if (profile == null)
                throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

            // 允许删除默认配置
            Profiles.Remove(profile);
            LastModified = DateTime.Now;
        }

        public void SetActiveProfile(string profileId)
        {
            var profile = GetProfileById(profileId);
            if (profile == null)
                throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

            ActiveProfileId = profileId;
            LastModified = DateTime.Now;
        }

        public List<HotkeyProfile> GetEnabledProfiles()
        {
            return Profiles.Where(p => p.IsEnabled).ToList();
        }

        public static ProfileConfiguration CreateDefault()
        {
            var config = new ProfileConfiguration();

            config.AddProfile(new HotkeyProfile
            {
                Id = "default-auto",
                Name = "自动语音转写",
                Hotkey = "F2",
                InputLanguage = "auto",
                OutputLanguage = "none",
                CustomPrompt = "",
                TranscriptionPrompt = "",
                TranslationPrompt = "",
                RecordingMode = "PushToTalk",
                IsDefault = true,
                IsEnabled = true
            });

            config.AddProfile(new HotkeyProfile
            {
                Id = "default-auto-zh",
                Name = "自动检测→中文（含校正效果）",
                Hotkey = "F3",
                InputLanguage = "auto",
                OutputLanguage = "zh-CN",
                CustomPrompt = "",
                TranscriptionPrompt = "",
                TranslationPrompt = @"---

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
  * Input: 我需要整理一下这个月市场部的budget",
                RecordingMode = "PushToTalk",
                IsDefault = true,
                IsEnabled = false
            });

            config.AddProfile(new HotkeyProfile
            {
                Id = "default-auto-en",
                Name = "自动检测→英文",
                Hotkey = "F4",
                InputLanguage = "auto",
                OutputLanguage = "en-US",
                CustomPrompt = "",
                TranscriptionPrompt = "",
                TranslationPrompt = "",
                RecordingMode = "PushToTalk",
                IsDefault = true,
                IsEnabled = false
            });

            config.AddProfile(new HotkeyProfile
            {
                Id = "default-auto-ja",
                Name = "自动检测→日文",
                Hotkey = "F5",
                InputLanguage = "auto",
                OutputLanguage = "ja-JP",
                CustomPrompt = "",
                TranscriptionPrompt = "",
                TranslationPrompt = "",
                RecordingMode = "PushToTalk",
                IsDefault = true,
                IsEnabled = false
            });

            return config;
        }
    }
}