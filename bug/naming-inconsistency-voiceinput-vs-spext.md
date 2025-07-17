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

## 修复步骤

### 第一阶段：用户可见部分（必须修复）
1. 修改窗口标题
   - SettingsWindow.xaml：将 "VoiceInput 设置" 改为 "Spext 设置"
   - MainWindow.xaml：将窗口标题改为 "Spext"

2. 修改系统托盘显示
   - App.xaml.cs：更新所有托盘提示文本
   - 将 "语音输入法" 改为 "Spext"

3. 修改提示消息
   - 启动提示、错误提示等所有用户可见文本

### 第二阶段：项目结构（建议修复）
1. 重命名项目文件夹：VoiceInput → Spext
2. 重命名项目文件：VoiceInput.csproj → Spext.csproj
3. 更新命名空间：namespace VoiceInput → namespace Spext
4. 更新类名：VoiceInputController → SpextController

### 第三阶段：系统集成（可选修复）
1. 更新注册表应用名
2. 更新凭据管理器标识
3. 更新日志文件名格式
4. 迁移用户配置路径

## 注意事项
1. **兼容性考虑**：修改系统集成部分可能影响现有用户的配置
2. **升级路径**：需要提供配置迁移功能
3. **版本控制**：大规模重命名会影响 Git 历史
4. **发布说明**：需要向用户说明品牌更名

## 建议修复优先级
1. **立即修复**：所有用户界面文本（窗口标题、托盘提示、消息）
2. **下个版本**：项目结构和命名空间
3. **评估后决定**：系统集成部分（需要考虑向后兼容）

## 影响评估
- **用户体验**：品牌一致性提升，更专业
- **开发影响**：需要更新所有引用，可能需要1-2天工作量
- **风险**：可能影响现有用户的配置和自动启动设置