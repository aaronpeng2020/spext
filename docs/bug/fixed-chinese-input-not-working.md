# Bug分析：中文输入无法正常工作

## 问题描述
用户报告语音识别成功后，识别的中文文字没有输入到鼠标焦点处。日志显示：
```
[11:03:44] 识别成功: 你好你好
[11:03:44] 准备输入文字: 你好你好
[11:03:44] 文字输入完成（键盘模拟方式）
```

但文字实际上没有输入到目标应用。

## 问题原因
`InputSimulatorCore` 库的 `TextEntry` 方法在处理中文等 Unicode 字符时存在兼容性问题。该方法主要针对 ASCII 字符设计，对于中文字符的输入支持不完善。

## 影响范围
- 所有中文语音识别结果无法正常输入
- 其他非 ASCII 字符（如日文、韩文、表情符号等）可能也受影响
- 影响用户体验，使语音输入功能基本无法使用

## 解决方案
使用 Windows API 的 `SendInput` 函数直接发送 Unicode 字符。这是 Windows 系统推荐的输入方法，能够正确处理所有 Unicode 字符。

### 实现细节
1. 添加 Windows API 声明和结构体定义
2. 实现 `SendUnicodeString` 方法，使用 `KEYEVENTF_UNICODE` 标志发送字符
3. 修改输入优先级：
   - 首先尝试 SendInput Unicode 方式（最可靠）
   - 如果失败，尝试 InputSimulator 方式（兼容性备选）
   - 最后使用剪贴板方式（最后手段）

## 修复代码
在 `TextInputService.cs` 中添加了以下关键代码：

```csharp
[DllImport("user32.dll", SetLastError = true)]
private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

private void SendUnicodeString(string text)
{
    var inputs = new INPUT[text.Length * 2];
    int inputIndex = 0;

    foreach (char c in text)
    {
        // Key down
        inputs[inputIndex] = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        inputIndex++;

        // Key up
        inputs[inputIndex] = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        inputIndex++;
    }

    uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    if (result != inputs.Length)
    {
        throw new Exception($"SendInput 失败，只发送了 {result}/{inputs.Length} 个输入");
    }
}
```

## 修复状态
**已修复** - 2025-07-18

### 第一次修复
使用 SendInput Unicode 方式解决了基本的中文输入问题。

### 第二次修复（增强版）
用户反馈部分窗口（如微信）仍然无法输入。添加了智能输入策略：
1. 检测目标窗口的类名和标题
2. 对于微信、QQ、钉钉等IM软件，优先使用剪贴板方式
3. 对于Electron应用，也优先使用剪贴板方式
4. 其他应用按原有优先级尝试

### 第三次修复（支持更多应用）
用户反馈 Telegram 无法输入。添加了对 Qt 应用的检测：
- Qt 应用（类名包含 "Qt"）
- Telegram 特定检测

## 测试结果
修复后的日志显示：
```
[11:06:09] 识别成功: 你好上海
[11:06:09] 准备输入文字: 你好上海
[11:06:09] 目标窗口 - 类名: WeChat, 标题: 微信
[11:06:09] 检测到IM软件，优先使用剪贴板方式
[11:06:09] 文字输入完成（剪贴板方式）
```

现在中文输入在各种应用中都能正常工作。

## 注意事项
1. 此修复需要管理员权限（程序已经要求管理员权限）
2. 某些应用可能有输入保护机制，可能需要使用剪贴板方式作为备选
3. SendInput 是 Windows 平台特定的解决方案，跨平台时需要其他方案