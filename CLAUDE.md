# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

SPEXT 输入法是一个基于 OpenAI Whisper API 的 Windows 语音输入法工具。用户按住热键录音，松开后自动将语音转换为文字并输入到当前光标位置。

**技术栈**：
- C# 9.0 + WPF (.NET Core 3.1)
- NAudio 2.2.1 (音频处理)
- OpenAI Whisper API (语音识别)
- Windows Forms (系统托盘)

## 常用开发命令

```bash
# 构建项目（在 VoiceInput 目录下执行）
dotnet build

# 运行项目
dotnet run
# 或使用批处理文件（在根目录）
run.bat

# 发布为自包含应用
dotnet publish -c Release -r win-x64 --self-contained

# 清理构建文件
dotnet clean
```

## 项目架构

### 核心模块
1. **VoiceInputController** (`Core/VoiceInputController.cs`) - 主控制器，协调所有服务
2. **GlobalHotkeyService** (`Services/GlobalHotkeyService.cs`) - 全局热键监听（默认F3）
3. **AudioRecorderService** (`Services/AudioRecorderService.cs`) - 音频录制（16kHz, 16位, 单声道）
4. **SpeechRecognitionService** (`Services/SpeechRecognitionService.cs`) - 调用 Whisper API
5. **AudioMuteService** (`Services/AudioMuteService.cs`) - 录音时静音其他音频
6. **TextInputService** (`Services/TextInputService.cs`) - 模拟键盘输入文字

### 服务注册和依赖注入
应用使用 Microsoft.Extensions.DependencyInjection 进行服务管理。所有服务在 `App.xaml.cs` 中注册为单例。

### 配置管理
- 配置文件：`appsettings.json`
- API密钥存储：使用 Windows 凭据管理器（通过 SecureStorageService）
- 用户配置保存路径：`%LOCALAPPDATA%\SPEXT\`

### 日志系统
- 日志文件路径：`%LOCALAPPDATA%\SPEXT\Logs\`
- 日志格式：`spext_{date}.log`
- 自动保留最近30天的日志

## 关键实现细节

1. **热键处理**：使用 Windows API `RegisterHotKey` 注册全局热键
2. **音频录制**：使用 NAudio 的 `WaveInEvent` 进行音频采集
3. **API调用**：支持代理配置，包括认证代理
4. **文本输入**：使用 InputSimulatorCore 模拟键盘输入
5. **系统托盘**：使用 Windows Forms 的 `NotifyIcon`

## 重要文件路径

- 主项目文件：`VoiceInput/VoiceInput.csproj`
- 配置文件：`VoiceInput/appsettings.json`
- 主窗口：`VoiceInput/Views/SettingsWindow.xaml`
- 应用入口：`VoiceInput/App.xaml.cs`
- 需求文档：`specs/voice-input/requirements.md`
- 设计文档：`specs/voice-input/design.md`

## 开发注意事项

1. **权限要求**：应用需要管理员权限运行（用于全局热键和模拟输入）
2. **平台限定**：仅支持 Windows（使用了 Windows 特定 API）
3. **框架版本**：必须使用 .NET Core 3.1（WPF 限制）
4. **代理配置**：如需访问 OpenAI API，可能需要配置代理
5. **安全存储**：API 密钥通过 Windows 凭据管理器安全存储，不要硬编码

## 调试技巧

1. 查看日志文件了解运行时错误
2. 使用 Visual Studio 的附加到进程功能调试系统托盘应用
3. 测试热键时注意其他应用可能占用相同热键
4. 音频问题可通过 NAudio 的音频设备枚举功能排查

## 最新功能改进（2025-01-19）

### 1. 设置页面宽度优化
- 将设置窗口宽度从 800 增加到 1200，提供更好的视觉体验

### 2. Prompt 配置重构
- 将 Prompt 配置功能从独立的按钮移到了快捷键编辑对话框中
- 移除了主页面的 "Prompt" 按钮，简化了操作流程

### 3. 转写和翻译 Prompt 分离
- 在 HotkeyProfile 模型中新增 `TranscriptionPrompt` 和 `TranslationPrompt` 字段
- 支持分别配置转写和翻译的提示词
- 保持向后兼容性，如果新字段为空会使用旧的 CustomPrompt

### 4. 翻译 Prompt 模板系统
- 新增 `TranslationPromptTemplates` 服务，提供丰富的翻译模板
- 支持多种语言对的专业翻译模板：
  - 中文→英文：专业翻译、商务翻译、技术文档翻译
  - 中文→日文：标准翻译、商务日语
  - 英文→中文：标准翻译、技术翻译、口语化翻译
  - 日文→中文：标准翻译
  - 中文→中文：文本润色、语音转文字优化、正式化、简化表达
  - 中文→繁体：简繁转换
  - 通用模板：基础翻译、保持格式、创意翻译

### 5. 智能模板选择
- 在编辑对话框中添加翻译模板下拉选择
- 根据输入和输出语言自动推荐合适的模板
- 支持自定义编辑模板内容

### 6. 特别注意：中文到中文的处理
- 当源语言和目标语言都是中文时，系统会使用特殊的提示词
- 确保输出始终是简体中文，不会出现其他语言
- 提供了多种中文优化模板：润色、纠错、正式化、简化等

### 7. 默认配置调整
- 保留四个默认配置：
  - F2：自动语音转写（默认启用）
  - F3：自动检测→中文（含校正效果）（默认禁用）
  - F4：自动检测→英文（默认禁用）
  - F5：自动检测→日文（默认禁用）
- F3 配置包含智能校正功能：
  - 修正错别字和同音字错误
  - 纠正基本语法错误
  - 添加适当的标点符号
  - 支持中英文混合输入输出
- 允许删除所有配置，包括默认配置
- 修复了编辑配置时的快捷键冲突检测问题
- 用户需要手动启用 F3、F4、F5 配置

### 8. 提示词长度限制优化
- 将提示词最大长度从 500 字符提高到 2000 字符
- 在编辑对话框中添加了实时字符计数显示
- 当字符数接近限制时会改变颜色提醒：
  - 0-1500：灰色
  - 1500-1800：橙色
  - 1800-2000：红色
- 使用 MaxLength 属性在输入时就限制长度，避免发送时被截断

### 9. F3 智能校正提示词优化
- 更新了 F3（自动检测→中文含校正效果）的默认提示词
- 新提示词包含详细的处理规则：
  - **基础处理规则**：纠正拼写、同音字、语法错误，添加标点
  - **中英文混合规则**：保持混合格式，不翻译嵌入的英文单词
  - **短文本处理规则**：不扩展短语，保持原意
  - **输入视角规则**：只校正不回复
  - **示例规则**：提供中英混合的处理示例
- 确保中英文混合输入时，英文单词（如 budget、meeting、review）不会被翻译成中文