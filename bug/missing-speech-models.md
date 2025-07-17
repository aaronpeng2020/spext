# Bug分析：缺少新的语音识别模型选项

## 问题描述
用户反馈 VoiceInput 应用中只支持 `whisper-1` 模型，但 OpenAI 现在提供了更多的语音转文字模型选项：
- `gpt-4o-transcribe`
- `gpt-4o-mini-transcribe`
- `whisper-1`

当前应用的模型选择下拉框中只有 `whisper-1` 一个选项。

## 问题分析

### 当前实现状况
1. **UI层面**：`ApiSettingsPage.xaml` 中的模型选择下拉框只有一个固定选项 "whisper-1 (默认)"
2. **配置层面**：`appsettings.json` 中硬编码了 `"Model": "whisper-1"`
3. **代码逻辑**：`ApiSettingsPage.xaml.cs` 第41行注释显示"目前只支持whisper-1，所以暂时不保存"
4. **API调用**：`SpeechRecognitionService.cs` 使用 `_configManager.WhisperModel` 获取模型名称

### 根本原因
应用开发时可能只有 `whisper-1` 模型可用，现在 OpenAI 增加了新模型但应用没有相应更新。

## 修复步骤

### 1. 更新UI界面 (`ApiSettingsPage.xaml`)
- 在 ComboBox 中添加新的模型选项
- 为每个模型添加简要说明（性能、成本等）

### 2. 更新配置管理 (`ConfigManager.cs`)
- 添加保存模型选择的方法 `SaveWhisperModel(string model)`
- 确保配置可以持久化保存

### 3. 更新设置页面逻辑 (`ApiSettingsPage.xaml.cs`)
- 实现模型选择的保存功能
- 根据配置加载当前选中的模型

### 4. 验证API兼容性
- 确认新模型的API端点是否相同
- 测试新模型的响应格式是否兼容

### 5. 更新默认配置
- 在 `appsettings.json` 中保留 `whisper-1` 作为默认值
- 添加配置说明文档

## 影响范围
- 用户体验：用户无法选择可能更适合其需求的新模型
- 性能：新模型可能提供更好的识别准确度或速度
- 成本：不同模型的定价可能不同

## 建议优先级
中等 - 这是一个功能增强，不影响现有功能的正常使用，但限制了用户选择。