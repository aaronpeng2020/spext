# Bug分析：项目命名不一致问题 - VoiceInput vs Spext

## 问题描述
项目的正式名称应该是 **Spext**（网址：Spext.ai），但在整个项目中大量使用了 "VoiceInput" 作为名称，造成品牌识别混乱。

## 影响范围

### 1. 用户可见部分（高优先级）
- **窗口标题**：显示 "VoiceInput 设置" 而非 "Spext 设置"
- **系统托盘**：显示 "语音输入法" 而非 "Spext"
- **提示信息**：各种通知和提示使用 "语音输入法" 或 "语音输入"
- **进程名称**：在任务管理器中显示为 VoiceInput.exe

### 2. 代码结构（中优先级）
- **命名空间**：所有 C# 文件使用 `namespace VoiceInput`
- **项目文件**：VoiceInput.csproj
- **主类名**：VoiceInputController、VoiceInput.App 等
- **文件夹名称**：项目根目录为 VoiceInput

### 3. 系统集成（中优先级）
- **注册表项**：自动启动使用 "VoiceInput" 作为应用名
- **凭据管理器**：API密钥存储使用 "VoiceInput_APIKey"
- **日志文件**：日志名为 VoiceInput_{date}.log
- **配置路径**：%LOCALAPPDATA%\VoiceInput\

### 4. 文档（低优先级）
- **CLAUDE.md**：项目概述仍使用 "VoiceInput"
- **部分 README**：只有主 README 使用了 "SPEXT"

## 已实施的修复

### 第一阶段：用户可见部分（已完成）
1. **修改窗口标题**
   - SettingsWindow.xaml：已将 "语音输入法设置" 改为 "Spext 设置"

2. **修改系统托盘显示**
   - TrayIcon.cs：已将所有 "语音输入法" 改为 "Spext"
   - 托盘提示文本现在显示 "Spext - 按住 F3 录音"

3. **修改提示消息**
   - App.xaml.cs：启动提示已改为 "Spext 正在启动..."、"Spext 启动成功！"
   - 单实例检查提示已改为 "Spext 已经在运行中。"
   - VoiceInputController.cs：所有提示消息已改为使用 "Spext"

4. **修改Mutex名称**
   - 将单实例检查的Mutex名从 "VoiceInput_SingleInstance" 改为 "Spext_SingleInstance"

## 修复状态
**部分修复** - 2025-07-18

已完成用户界面相关的品牌名称更新，用户现在看到的所有文本都显示为 "Spext"。

## 剩余工作

### 第二阶段：项目结构（未实施）
由于涉及大规模代码重构，建议在下个版本中进行：
1. 重命名项目文件夹：VoiceInput → Spext
2. 重命名项目文件：VoiceInput.csproj → Spext.csproj
3. 更新命名空间：namespace VoiceInput → namespace Spext
4. 更新类名：VoiceInputController → SpextController

### 第三阶段：系统集成（未实施）
需要考虑向后兼容性：
1. 更新注册表应用名
2. 更新凭据管理器标识
3. 更新日志文件名格式
4. 迁移用户配置路径

## 注意事项
1. **兼容性考虑**：当前修复不影响现有用户的配置和设置
2. **进程名称**：由于项目文件名未改，进程仍显示为 VoiceInput.exe
3. **系统集成**：凭据管理器和配置路径保持不变，确保兼容性

## 影响评估
- **用户体验**：✅ 用户界面已统一显示 "Spext" 品牌
- **开发影响**：代码结构暂未更改，不影响开发
- **风险**：最小化，仅修改了显示文本，未触及核心功能

## 后续建议
1. 在下个主要版本中完成项目结构重命名
2. 提供配置迁移工具，帮助用户从旧路径迁移到新路径
3. 在发布说明中明确说明品牌更名
4. 考虑保留部分兼容性代码，支持旧版本升级