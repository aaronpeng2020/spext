# Bug 分析：缺少 Whisper 多模型选择支持

## 问题描述

用户反馈 SPEXT 当前只支持 `whisper-1` 模型，而 OpenAI 已经推出了新的转写模型：
- `gpt-4o-mini-transcribe`：低延迟、低成本版本
- `gpt-4o-transcribe`：高精度版本

用户希望在设置页面能够选择使用哪个转写模型。

## 当前实现分析

1. **模型配置位置**：
   - 配置文件：`VoiceInput/appsettings.json` 中的 `WhisperAPI.Model` 字段
   - 配置管理：`ConfigManager.cs` 的 `WhisperModel` 属性（第71行）
   - 当前默认值：`"whisper-1"`

2. **模型使用位置**：
   - `EnhancedSpeechRecognitionService.cs` 第103行：通过 `_configManager.WhisperModel` 读取模型配置
   - 在 API 调用时作为 `model` 参数发送

3. **设置页面**：
   - 当前设置页面没有提供模型选择的 UI 控件

## 修复方案

### 1. 更新配置文件
在 `appsettings.json` 中保持现有的 `WhisperAPI.Model` 字段，用户可通过设置页面修改。

### 2. 添加模型选择 UI
在设置页面的"语音和转写设置"部分添加一个下拉框，包含三个选项：
- `whisper-1` - 经典模型（平衡性能）
- `gpt-4o-mini-transcribe` - 快速模型（低延迟、低成本）
- `gpt-4o-transcribe` - 高精度模型（最佳质量）

### 3. 修改文件列表

1. **SettingsWindow.xaml**
   - 在"语音和转写设置"区域添加模型选择下拉框
   - 添加模型说明文本

2. **SettingsWindow.xaml.cs**
   - 添加模型选择的数据绑定
   - 处理模型选择变更事件
   - 保存模型配置到 `ConfigManager`

3. **ConfigManager.cs**
   - 无需修改，现有的 `WhisperModel` 属性已经支持读写

4. **EnhancedSpeechRecognitionService.cs**
   - 无需修改，已经使用 `_configManager.WhisperModel`

### 4. 实现细节

#### UI 布局（SettingsWindow.xaml）
```xml
<!-- 在输入语言选择之后添加 -->
<StackPanel Margin="0,10,0,0">
    <TextBlock Text="转写模型：" FontWeight="SemiBold" Margin="0,0,0,5"/>
    <ComboBox x:Name="WhisperModelComboBox" 
              Width="300" 
              HorizontalAlignment="Left"
              SelectionChanged="WhisperModelComboBox_SelectionChanged">
        <ComboBoxItem Tag="whisper-1">whisper-1 (经典模型 - 平衡性能)</ComboBoxItem>
        <ComboBoxItem Tag="gpt-4o-mini-transcribe">gpt-4o-mini-transcribe (快速模型 - 低延迟)</ComboBoxItem>
        <ComboBoxItem Tag="gpt-4o-transcribe">gpt-4o-transcribe (高精度模型 - 最佳质量)</ComboBoxItem>
    </ComboBox>
    <TextBlock Text="提示：快速模型适合实时字幕，高精度模型适合复杂口音场景" 
               Foreground="Gray" 
               FontSize="12" 
               Margin="0,5,0,0"/>
</StackPanel>
```

#### 代码逻辑（SettingsWindow.xaml.cs）
```csharp
// 在 LoadConfiguration 方法中添加
private void LoadWhisperModelSelection()
{
    var currentModel = _configManager.WhisperModel;
    foreach (ComboBoxItem item in WhisperModelComboBox.Items)
    {
        if (item.Tag?.ToString() == currentModel)
        {
            WhisperModelComboBox.SelectedItem = item;
            break;
        }
    }
}

// 添加选择变更处理
private void WhisperModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (WhisperModelComboBox.SelectedItem is ComboBoxItem selectedItem)
    {
        var model = selectedItem.Tag?.ToString();
        if (!string.IsNullOrEmpty(model))
        {
            _configManager.SaveSetting("VoiceInput:WhisperAPI:Model", model);
        }
    }
}
```

### 5. 测试要点

1. 验证三个模型都能正常调用 API
2. 验证配置保存和加载是否正确
3. 测试不同模型的响应速度和识别准确度
4. 确保 API 兼容性（新模型使用相同的 API endpoint）

### 6. 风险评估

- **低风险**：只是添加模型选择，不影响现有功能
- **兼容性**：保持默认值为 `whisper-1`，确保向后兼容
- **API 成本**：需要提醒用户不同模型的价格差异

### 7. 预期效果

用户可以根据使用场景选择合适的模型：
- 日常使用选择 `whisper-1`
- 需要快速响应选择 `gpt-4o-mini-transcribe`
- 处理方言或噪音环境选择 `gpt-4o-transcribe`