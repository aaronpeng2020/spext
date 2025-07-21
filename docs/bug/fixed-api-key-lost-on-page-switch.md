# Bug: API密钥在设置页面切换时丢失

## 问题描述
在设置界面切换不同的设置选项时（比如从基本设置切换到API设置，再切回基本设置），API密钥输入框的内容会被清空。虽然没有保存，但输入框里的内容不见了。

## 问题原因
1. WPF的`PasswordBox`控件在从视觉树中移除时（页面切换导致），会自动清空其内容，这是出于安全考虑的设计
2. 设置窗口使用`Frame.Navigate()`切换页面，会导致页面实例从视觉树中移除
3. 虽然页面实例被保持（没有重新创建），但`PasswordBox`的密码已经被清空

## 解决方案
在`BasicSettingsPage`中添加临时变量保存API密钥：

1. 添加私有字段`_tempApiKey`用于临时保存密码
2. 监听`PasswordBox.PasswordChanged`事件，实时保存用户输入到临时变量
3. 监听页面`Loaded`事件，在页面重新加载时恢复密码
4. 在`LoadSettings()`方法中也更新临时变量

## 代码修改

### BasicSettingsPage.xaml.cs
```csharp
private string _tempApiKey; // 临时保存API密钥，防止页面切换时丢失

public BasicSettingsPage(ConfigManager configManager)
{
    // ... 原有代码 ...
    
    // 监听密码框变化，保存到临时变量
    ApiKeyBox.PasswordChanged += (s, e) => 
    {
        _tempApiKey = ApiKeyBox.Password;
    };
    
    // 监听页面加载事件，恢复密码
    Loaded += (s, e) => 
    {
        if (!string.IsNullOrEmpty(_tempApiKey))
        {
            ApiKeyBox.Password = _tempApiKey;
        }
        else if (!string.IsNullOrEmpty(_configManager.ApiKey))
        {
            ApiKeyBox.Password = _configManager.ApiKey;
        }
    };
}
```

## 测试验证
1. 打开设置窗口
2. 在基本设置中输入API密钥（不保存）
3. 切换到API设置页面
4. 切换回基本设置页面
5. 验证API密钥仍然显示在输入框中

## 相关文件
- `VoiceInput/Views/Pages/BasicSettingsPage.xaml.cs`
- `VoiceInput/Views/SettingsWindow.xaml.cs`

## 状态
已修复