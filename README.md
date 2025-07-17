# SPEXT VoiceInput - Windows 语音输入法

一款基于 OpenAI Whisper API 的 Windows 语音输入工具，支持全局热键录音并自动转换为文字输入。

## 功能特点

- 🎤 **全局热键录音**：按住 F3 键即可开始录音，松开自动识别
- 🔇 **智能静音**：录音时自动暂停系统其他音频，录音结束后恢复
- 🌐 **代理支持**：支持 HTTP 代理配置，包括需要认证的代理
- 📝 **即时输入**：识别完成后自动输入文字到当前光标位置
- 🔒 **安全存储**：API 密钥使用 Windows 凭据管理器安全存储
- 🚀 **开机自启**：支持设置开机自动启动
- 📊 **系统托盘**：最小化到系统托盘，不占用任务栏空间

## 系统要求

- Windows 10 或更高版本
- .NET Core 3.1 运行时
- OpenAI API 密钥

## 安装与配置

1. **下载程序**
   - 下载最新版本的 VoiceInput
   - 解压到任意目录

2. **配置 API 密钥**
   - 右键点击系统托盘图标，选择"设置"
   - 在"基本设置"标签页中输入你的 OpenAI API 密钥
   - 点击"测试语音识别"验证密钥是否有效

3. **配置代理（可选）**
   - 切换到"代理设置"标签页
   - 勾选"启用HTTP代理"
   - 输入代理服务器地址和端口
   - 如需认证，勾选"代理需要身份验证"并输入用户名密码
   - 点击"测试代理连接"验证配置

## 使用方法

1. **启动程序**
   - 运行 `VoiceInput.exe`
   - 程序会最小化到系统托盘

2. **语音输入**
   - 将光标放置在需要输入文字的位置
   - 按住 `F3` 键开始录音（系统会自动静音其他音频）
   - 说话完成后松开 `F3` 键
   - 等待识别完成，文字会自动输入到光标位置

3. **查看日志**
   - 右键点击系统托盘图标，选择"查看日志"
   - 日志文件位置：`%LOCALAPPDATA%\VoiceInput\Logs\`

## 配置文件说明

配置文件 `appsettings.json` 包含以下设置：

```json
{
  "VoiceInput": {
    "Hotkey": "F3",                    // 录音快捷键
    "AutoStart": true,                  // 开机自动启动
    "MuteWhileRecording": true,         // 录音时静音其他音频
    "AudioSettings": {
      "SampleRate": 16000,              // 采样率
      "BitsPerSample": 16,              // 位深度
      "Channels": 1                     // 声道数（单声道）
    },
    "WhisperAPI": {
      "Model": "whisper-1",             // 使用的模型
      "BaseUrl": "https://api.openai.com/v1/audio/transcriptions",
      "Timeout": 30                     // API 超时时间（秒）
    },
    "ProxySettings": {
      "Enabled": false,                 // 是否启用代理
      "Address": "",                    // 代理地址
      "Port": 0,                        // 代理端口
      "RequiresAuthentication": false   // 是否需要认证
    }
  }
}
```

## 故障排除

### 录音没有反应
- 检查麦克风是否正常工作
- 确保程序有麦克风访问权限
- 查看日志文件是否有错误信息

### API 调用失败
- 检查 API 密钥是否正确
- 检查网络连接是否正常
- 如使用代理，确保代理配置正确

### 静音功能不工作
- 某些应用可能不受音频会话控制
- 可以在设置中关闭"录音时暂停其他声音"选项

### F3 键冲突
- 如果 F3 键与其他软件冲突，暂时不支持自定义快捷键
- 可以临时关闭冲突的软件

## 隐私说明

- API 密钥存储在 Windows 凭据管理器中，不会明文保存
- 录音数据仅用于语音识别，不会保存音频文件
- 所有语音识别通过 OpenAI API 进行，请参考 OpenAI 的隐私政策

## 开发说明

### 技术栈
- C# + WPF（.NET Core 3.1）
- NAudio - 音频录制和处理
- Newtonsoft.Json - JSON 处理
- Windows Credential Manager - 安全存储

### 主要模块
- `AudioRecorderService` - 音频录制服务
- `SpeechRecognitionService` - 语音识别服务
- `AudioMuteService` - 音频静音服务
- `GlobalHotkeyService` - 全局热键服务
- `ConfigManager` - 配置管理
- `VoiceInputController` - 主控制器

## 更新日志

### v1.0.0
- 初始版本发布
- 支持 F3 热键录音
- 支持 OpenAI Whisper API
- 支持代理配置
- 支持录音时静音
- 修复 F3 键在终端中产生字符的问题
- 修复代理设置无法保存的问题

## 许可证

本项目采用 MIT 许可证。

## 联系方式

如有问题或建议，请提交 Issue。