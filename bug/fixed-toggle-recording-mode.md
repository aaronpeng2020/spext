# 功能需求分析：支持切换式录音模式

## 需求描述
用户希望系统同时支持两种录音模式：
1. **按住模式（Push-to-Talk）**：按住快捷键开始录音，松开时停止录音（当前已实现）
2. **切换模式（Toggle）**：轻按快捷键开始录音，再次轻按停止录音（新需求）

## 当前实现分析

### 1. 热键服务（GlobalHotkeyService.cs）
- 使用低级键盘钩子监听快捷键
- 在 `HookCallback` 方法中：
  - `WM_KEYDOWN` 时触发 `HotkeyPressed` 事件，参数为 true
  - `WM_KEYUP` 时触发 `HotkeyPressed` 事件，参数为 false

### 2. 控制器（VoiceInputController.cs）
- 在 `OnHotkeyPressed` 方法中：
  - `isPressed = true` 时：开始录音
  - `isPressed = false` 时：停止录音

### 3. 当前模式的优点
- 简单直观，符合对讲机使用习惯
- 不需要记住录音状态
- 误操作概率低

## 技术可行性分析

实现切换模式需要：
1. 在配置中添加录音模式选项
2. 维护录音状态（是否正在录音）
3. 根据模式和状态决定操作

## 实现方案

### 方案一：配置文件增加模式选项
1. 在 `appsettings.json` 中添加：
   ```json
   "RecordingMode": "PushToTalk" // 或 "Toggle"
   ```

2. 在 `ConfigManager` 中添加对应属性

3. 修改 `VoiceInputController.OnHotkeyPressed` 方法：
   ```csharp
   private bool _isRecording = false;
   
   private void OnHotkeyPressed(object? sender, bool isPressed)
   {
       if (_configManager.RecordingMode == "PushToTalk")
       {
           // 当前逻辑
           if (isPressed) StartRecording();
           else StopRecording();
       }
       else if (_configManager.RecordingMode == "Toggle")
       {
           // 只在按下时处理，忽略释放事件
           if (isPressed)
           {
               if (!_isRecording) StartRecording();
               else StopRecording();
               _isRecording = !_isRecording;
           }
       }
   }
   ```

### 方案二：每个热键配置独立模式
如果使用了多热键配置（HotkeyProfile），可以为每个热键单独设置模式，提供更大的灵活性。

### 方案三：智能模式
同时支持两种模式：
- 短按（< 500ms）：切换模式
- 长按（>= 500ms）：按住模式

## 修复步骤

1. **更新配置模型**
   - 在 `appsettings.json` 添加 RecordingMode 配置
   - 在 `ConfigManager.cs` 添加对应属性
   - 在 `HotkeyProfile.cs` 添加 RecordingMode 属性（如果支持每个热键独立配置）

2. **更新 UI**
   - 在设置界面添加录音模式选择（下拉框或单选按钮）
   - 在热键编辑对话框中添加模式选择（如果支持独立配置）

3. **修改控制逻辑**
   - 在 `VoiceInputController` 或 `EnhancedVoiceInputController` 中添加录音状态跟踪
   - 根据配置的模式处理热键事件

4. **测试验证**
   - 测试按住模式是否正常工作
   - 测试切换模式是否正常工作
   - 测试模式切换是否生效
   - 测试多热键场景下的独立配置

## 注意事项

1. **状态同步**：确保录音状态与实际状态一致，避免出现状态不同步
2. **视觉反馈**：切换模式下需要更明显的录音状态指示
3. **误操作处理**：切换模式下可能出现忘记停止录音的情况
4. **向后兼容**：确保更新不影响现有用户的使用习惯

## 建议实现优先级

1. 先实现全局配置（所有热键使用相同模式）
2. 再考虑每个热键独立配置
3. 最后考虑智能模式（可选功能）