# Bug分析：输入焦点丢失问题

## 问题描述
用户反馈：当按下语音输入快捷键时，应该记录输入光标当前所处的位置。即便后续输入焦点已经移开，变成其他窗口，也应该将内容输入到当时按键的输入框。

## 问题分析

### 现有实现
通过分析 `TextInputService.cs` 和控制器代码，发现当前实现存在以下问题：

1. **焦点获取时机错误**：
   - 在 `TextInputService.TypeText()` 方法中（第92行），是在文字准备输入时才获取当前焦点窗口
   - 这时距离用户按下快捷键已经过了较长时间（录音+识别处理）
   - 用户可能已经切换到其他窗口

2. **缺少焦点记录机制**：
   - 在 `VoiceInputController.OnHotkeyPressed()` 和 `EnhancedVoiceInputController.OnHotkeyPressed()` 中
   - 只处理了录音的开始/停止，没有记录按键时的焦点窗口

3. **输入目标不正确**：
   - 文字总是输入到"当前"焦点窗口，而不是用户按键时的窗口
   - 这导致如果用户在等待识别过程中切换窗口，文字会输入到错误位置

### 根本原因
系统没有在用户按下快捷键的瞬间记录当时的输入焦点，而是在识别完成后才获取焦点，这时焦点可能已经改变。

## 修复方案

### 方案一：记录按键时的焦点（推荐）
1. 在 `OnHotkeyPressed` 中，当 `isPressed = true` 时立即记录当前焦点窗口句柄
2. 将窗口句柄保存在控制器实例变量中
3. 修改 `TextInputService` 添加接受目标窗口句柄的重载方法
4. 在识别完成后，将文字发送到记录的窗口而非当前窗口

### 方案二：使用 SetForegroundWindow（备选）
1. 在输入前使用 Windows API `SetForegroundWindow` 恢复原窗口焦点
2. 但这种方式会造成窗口切换闪烁，用户体验较差

## 修复步骤

### 1. 修改 TextInputService
- 添加新方法 `TypeTextToWindow(string text, IntPtr targetWindow)`
- 使用 `SendMessage` 或 `PostMessage` API 直接向目标窗口发送文本

### 2. 修改控制器
- 在 `VoiceInputController` 和 `EnhancedVoiceInputController` 中添加 `_targetWindow` 字段
- 在 `OnHotkeyPressed` 按下时记录：`_targetWindow = GetForegroundWindow()`
- 在 `OnRecordingCompleted` 中使用：`_textInput.TypeTextToWindow(text, _targetWindow)`

### 3. 添加窗口句柄验证
- 在输入前验证窗口句柄是否仍然有效（`IsWindow` API）
- 如果窗口已关闭，回退到当前焦点窗口

### 4. 处理特殊应用
- 某些应用（如浏览器）可能需要记录具体的输入控件句柄
- 使用 `GetFocus` API 获取具体的编辑控件

## 测试场景
1. 在记事本中按下快捷键，录音过程中切换到其他窗口
2. 在浏览器输入框按下快捷键，录音时切换标签页
3. 在聊天软件中使用，录音时切换聊天窗口
4. 录音过程中关闭原窗口的处理

## 预期效果
无论用户在录音和识别过程中如何切换窗口，文字都应该准确输入到按键时的位置，除非该窗口已被关闭。