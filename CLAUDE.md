# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

VoiceInput 是一个基于 OpenAI Whisper API 的 Windows 语音输入法工具。用户按住热键录音，松开后自动将语音转换为文字并输入到当前光标位置。

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
- 用户配置保存路径：`%LOCALAPPDATA%\VoiceInput\`

### 日志系统
- 日志文件路径：`%LOCALAPPDATA%\VoiceInput\Logs\`
- 日志格式：`voiceinput_{date}.log`
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