# Bug分析：API语言设置保存失败且API密钥消失

## 问题描述
用户在保存API设置中的语言设置时，保存后重新打开设置窗口，发现：
1. 语言设置没有被保存
2. 基本设置中的API密钥也消失了

## 问题原因分析

### 1. **语言设置保存逻辑未实现**
在 `ApiSettingsPage.xaml.cs` 的 `SaveSettings()` 方法中（第44-67行），语言设置的保存逻辑被注释掉了：

```csharp
// TODO: 保存语言设置
var selectedLanguage = (InputLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
var selectedMode = (OutputModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

// TODO: 保存Temperature设置
```

关键的保存代码都是TODO状态，实际上没有执行任何保存操作。

### 2. **API密钥消失的原因**
这是一个更严重的问题。在 `SettingsWindow.xaml.cs` 的 `SaveButton_Click` 方法中（第59-77行），系统会依次调用所有页面的 `SaveSettings()` 方法：

```csharp
_basicPage.SaveSettings();  // 保存基本设置（包括API密钥）
_apiPage.SaveSettings();    // 这里可能出错
_proxyPage.SaveSettings();  
_uiPage.SaveSettings();
```

如果 `_apiPage.SaveSettings()` 在执行过程中抛出异常，可能会导致整个保存流程中断或状态不一致。

### 3. **ConfigManager的潜在问题**
在 `BasicSettingsPage.SaveSettings()` 中（第40-53行），API密钥是通过以下方式保存的：
```csharp
_configManager.ApiKey = ApiKeyBox.Password;
```

而 `ConfigManager.ApiKey` 的setter会将密钥保存到Windows凭据管理器。如果在保存过程中发生异常或者配置重载，可能导致密钥丢失。

### 4. **配置文件结构缺失**
当前的 `appsettings.json` 中没有语言相关的配置项定义，这意味着即使实现了保存逻辑，也没有地方存储这些设置。

## 修复步骤

### 1. **完善配置文件结构**
在 `appsettings.json` 中添加语言相关配置：
```json
{
  "VoiceInput": {
    "WhisperAPI": {
      "Model": "whisper-1",
      "BaseUrl": "https://api.openai.com/v1/audio/transcriptions",
      "Timeout": 30,
      "Language": "auto",      // 新增
      "Temperature": 0.0,      // 新增
      "OutputMode": "text"     // 新增
    }
  }
}
```

### 2. **在ConfigManager中添加语言设置属性**
```csharp
public string WhisperLanguage { get; set; }
public double WhisperTemperature { get; set; }
public string WhisperOutputMode { get; set; }
```

### 3. **实现ApiSettingsPage的保存逻辑**
将TODO注释的代码实现：
```csharp
public void SaveSettings()
{
    // 保存API URL
    var apiUrl = ApiUrlBox.Text.Trim();
    if (!string.IsNullOrEmpty(apiUrl))
    {
        _configManager.SaveSetting("VoiceInput:WhisperAPI:BaseUrl", apiUrl);
    }

    // 保存语言设置
    var selectedLanguage = (InputLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    if (!string.IsNullOrEmpty(selectedLanguage))
    {
        _configManager.SaveSetting("VoiceInput:WhisperAPI:Language", selectedLanguage);
    }

    // 保存输出模式
    var selectedMode = (OutputModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    if (!string.IsNullOrEmpty(selectedMode))
    {
        _configManager.SaveSetting("VoiceInput:WhisperAPI:OutputMode", selectedMode);
    }

    // 保存Temperature设置
    if (double.TryParse(TemperatureBox.Text, out double temperature))
    {
        _configManager.SaveSetting("VoiceInput:WhisperAPI:Temperature", temperature.ToString());
    }
}
```

### 4. **添加异常处理**
在 `SettingsWindow.SaveButton_Click` 中为每个页面的保存添加独立的异常处理：
```csharp
private void SaveButton_Click(object sender, RoutedEventArgs e)
{
    var errors = new List<string>();
    
    try { _basicPage.SaveSettings(); }
    catch (Exception ex) { errors.Add($"基本设置: {ex.Message}"); }
    
    try { _apiPage.SaveSettings(); }
    catch (Exception ex) { errors.Add($"API设置: {ex.Message}"); }
    
    // ... 其他页面
    
    if (errors.Any())
    {
        MessageBox.Show($"部分设置保存失败:\n{string.Join("\n", errors)}", "警告");
    }
}
```

### 5. **确保加载逻辑也正确实现**
在 `ApiSettingsPage.LoadSettings()` 中加载保存的语言设置。

## 影响评估
- **严重性**：高 - API密钥丢失会导致功能完全无法使用
- **影响范围**：所有使用API设置页面的用户
- **修复优先级**：紧急

## 测试建议
1. 测试保存各种语言设置组合
2. 测试保存时故意制造异常，验证API密钥不会丢失
3. 测试设置的持久化和重启后的加载
4. 测试并发保存场景