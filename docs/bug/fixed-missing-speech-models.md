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

## 已实施的修复

### 1. 更新了UI界面 (`ApiSettingsPage.xaml`)
在模型选择下拉框中添加了新的模型选项：
- whisper-1 (经典模型)
- gpt-4o-mini-transcribe (快速，成本低)
- gpt-4o-transcribe (最准确)

每个选项都添加了简要说明，帮助用户选择。

### 2. 实现了模型保存功能 (`ApiSettingsPage.xaml.cs`)
- 添加了 `SetModelSelection()` 方法用于加载已保存的模型选择
- 在 `SaveSettings()` 中实现了模型选择的保存逻辑
- 模型选择会保存到配置文件中

### 3. 改进了配置管理
- 将 `ConfigManager.SaveSetting()` 方法改为公开，允许保存任意配置项
- 模型配置会持久化保存到 `appsettings.json`

### 4. 保持了API兼容性
- `SpeechRecognitionService` 已经在使用 `_configManager.WhisperModel`
- 新模型使用相同的API端点，无需修改API调用逻辑

## 修复状态
**已修复** - 2025-07-17

## 测试建议
1. 验证三个模型选项都能正确显示
2. 测试选择不同模型后保存，重新打开设置确认选择被保存
3. 使用不同模型进行语音识别，验证功能正常
4. 检查日志中显示的是正确的模型名称

## 影响范围
- **用户体验**：用户现在可以根据需求选择不同的模型
- **性能**：新模型可能提供更好的识别准确度或速度
- **成本**：不同模型的定价不同，用户可以根据预算选择

## 后续建议
1. 在文档中说明不同模型的特点和适用场景
2. 考虑添加模型性能对比说明
3. 可以在UI中显示当前选择模型的预估成本