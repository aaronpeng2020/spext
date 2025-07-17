# 语音输入法

SPEXT 一个基于 Windows 的语音输入法软件，使用 OpenAI Whisper API 进行语音识别。

## 功能特点

- 全局快捷键录音（默认 F3）
- 系统托盘运行
- 自动语言检测
- 开机自动启动
- 支持长时间录音

## 使用方法

1. 首次运行需要配置 OpenAI API 密钥
2. 右键点击系统托盘图标，选择"设置"
3. 输入您的 OpenAI API 密钥
4. 按住 F3 键开始录音，松开结束录音
5. 识别结果会自动输入到当前光标位置

## 开发环境

- .NET Core 3.1
- Visual Studio 2019/2022
- Windows 10/11

## 构建方法

```bash
dotnet restore
dotnet build
dotnet run
```

## 已完成功能

- ✅ 项目基础架构
- ✅ 系统托盘功能
- ✅ 单实例检查
- ✅ 全局快捷键监听（按下/释放）
- ✅ 音频录制服务
- ✅ OpenAI Whisper API 集成
- ✅ 语音识别功能
- ✅ 文字输入服务（支持中文）
- ✅ 设置界面
- ✅ API 密钥安全存储（Windows Credential Manager）
- ✅ 开机自动启动功能
- ✅ 详细的日志输出
- ✅ 完善的错误处理
- ✅ API 连接测试功能

## 待完成功能

- ⏳ 性能优化
- ⏳ 打包发布
- ⏳ 应用图标设计
- ⏳ 多语言支持（界面）
- ⏳ 历史记录功能

## 注意事项

- 需要有效的 OpenAI API 密钥
- 需要网络连接
- 仅支持 Windows 系统