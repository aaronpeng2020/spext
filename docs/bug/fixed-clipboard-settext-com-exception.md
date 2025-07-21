# Bug分析：剪贴板SetText操作COM异常 [已修复]

## 问题描述
用户在使用语音输入功能时遇到剪贴板操作失败的错误。错误发生在 `TextInputService.UseClipboardInput` 方法中尝试设置剪贴板文本时。

## 错误信息
```
System.Runtime.InteropServices.COMException
   at System.Windows.Clipboard.SetDataInternal(String format, Object data)
   at System.Windows.Clipboard.SetText(String text)
   at VoiceInput.Services.TextInputService.<>c__DisplayClass22_0.<UseClipboardInput>b__1() 
   in D:\code\playground-spext\VoiceInput\Services\TextInputService.cs:line 269
```

## 问题分析

### 可能原因
1. **剪贴板资源竞争**
   - 其他应用程序（如剪贴板管理器、杀毒软件等）正在访问剪贴板
   - Windows系统剪贴板服务暂时不可用

2. **线程安全问题**
   - 虽然使用了 `Dispatcher.Invoke`，但可能存在其他线程同时访问剪贴板

3. **剪贴板状态异常**
   - 剪贴板可能处于某种异常状态，需要清理或重试

## 修复方案

### 方案1：添加重试机制（推荐）
在剪贴板操作失败时进行重试，避免偶发性失败：

```csharp
private void UseClipboardInput(string text)
{
    // ... 备份剪贴板内容的代码 ...

    const int maxRetries = 3;
    const int retryDelay = 100;
    Exception? lastException = null;

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Clipboard.SetText(text);
            });
            
            // 成功则跳出循环
            break;
        }
        catch (Exception ex)
        {
            lastException = ex;
            LoggerService.Log($"设置剪贴板失败（尝试 {i + 1}/{maxRetries}）: {ex.Message}");
            
            if (i < maxRetries - 1)
            {
                System.Threading.Thread.Sleep(retryDelay);
            }
        }
    }

    if (lastException != null)
    {
        LoggerService.Log($"设置剪贴板最终失败: {lastException.Message}");
        throw new Exception("无法设置剪贴板内容", lastException);
    }

    // ... 后续的粘贴操作 ...
}
```

### 方案2：使用剪贴板API的Try模式
使用更安全的方式操作剪贴板：

```csharp
private bool TrySetClipboardText(string text, int timeoutMs = 1000)
{
    var endTime = DateTime.Now.AddMilliseconds(timeoutMs);
    
    while (DateTime.Now < endTime)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetDataObject(text, true);
            });
            return true;
        }
        catch (COMException)
        {
            System.Threading.Thread.Sleep(50);
        }
    }
    
    return false;
}
```

### 方案3：提供降级方案
当剪贴板方式失败时，回退到逐字符输入：

```csharp
catch (Exception ex)
{
    LoggerService.Log($"剪贴板输入失败: {ex.Message}，尝试逐字符输入");
    // 回退到SendKeys或其他输入方式
    foreach (char c in text)
    {
        _inputSimulator.Keyboard.TextEntry(c);
        System.Threading.Thread.Sleep(10);
    }
}
```

## 临时解决方案
用户可以尝试以下临时解决方案：
1. 关闭可能占用剪贴板的程序（如剪贴板管理器）
2. 重启SPEXT应用程序
3. 在设置中禁用"优先使用剪贴板输入"选项（如果有）

## 建议修复优先级
**高** - 此问题直接影响核心功能的使用体验，建议尽快修复。

## 测试建议
修复后需要测试以下场景：
1. 正常情况下的剪贴板输入
2. 剪贴板被其他程序占用时的表现
3. 快速连续使用语音输入
4. 系统资源紧张时的稳定性

## 修复状态
**已修复** - 2025-01-19

### 实施的修复方案
采用了方案1（重试机制），在 `TextInputService.UseClipboardInput` 方法中添加了：
1. 设置剪贴板时的3次重试机制，每次重试间隔100ms
2. 恢复剪贴板时的3次重试机制，确保原内容能够正确恢复
3. 详细的日志记录，帮助诊断问题
4. 友好的错误提示，告知用户可能的原因

### 修复效果
- 避免了偶发性的剪贴板访问失败
- 提供了更好的错误处理和日志记录
- 即使在剪贴板被占用时也能在重试后成功