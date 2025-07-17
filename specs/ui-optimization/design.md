# 技术方案设计 - 界面UI优化

## 概述

本方案旨在对VoiceInput应用进行全面的UI现代化改造，在保持现有.NET Core 3.1 + WPF技术栈的基础上，通过引入现代UI库、自定义控件和动画效果，实现更美观、更专业的用户界面。

## 技术架构

### 1. UI框架选型

考虑到项目使用.NET Core 3.1的限制，选择以下技术方案：

- **ModernWpfUI** - 微软官方的现代化WPF控件库
  - 提供Fluent Design风格的控件
  - 支持亮色/暗色主题
  - 兼容.NET Core 3.1
  - 轻量级，易于集成

- **WPF动画框架**
  - 使用原生WPF Storyboard实现动画效果
  - 利用Easing Functions实现平滑过渡
  - 自定义动画行为

### 2. 组件设计

#### 2.1 现代化设置窗口
```
设置窗口
├── 标题栏（自定义，支持拖动）
├── 导航栏（侧边栏式）
│   ├── 基本设置
│   ├── API设置
│   ├── 代理设置
│   └── 界面设置
├── 内容区域（带过渡动画）
└── Logo区域（右下角固定）
```

#### 2.2 音频波形可视化窗口
```
波形窗口
├── 透明背景层
├── 波形显示区
│   ├── 频谱分析器（实时音频数据）
│   └── 波形动画（Canvas绘制）
└── 窗口管理（始终置顶，不获取焦点）
```

### 3. 技术实现细节

#### 3.1 样式和主题系统
- 创建资源字典文件结构：
  ```
  Resources/
  ├── Themes/
  │   ├── LightTheme.xaml
  │   ├── DarkTheme.xaml
  │   └── Common.xaml
  ├── Styles/
  │   ├── ButtonStyles.xaml
  │   ├── TextBoxStyles.xaml
  │   └── WindowStyles.xaml
  └── Icons/
      ├── app-icon.ico
      └── logo.png
  ```

#### 3.2 音频可视化技术
- 使用NAudio的音频数据流
- 实时FFT（快速傅里叶变换）分析
- Canvas + DrawingVisual高性能绘制
- 60 FPS刷新率的平滑动画

#### 3.3 配置扩展
- 扩展ConfigManager支持新的配置项：
  - 界面主题（亮色/暗色）
  - 波形显示开关
  - 语言设置（输入语言、输出模式）
  - API高级参数

### 4. 数据模型设计

#### 4.1 配置文件结构扩展
```json
{
  "VoiceInput": {
    "UI": {
      "Theme": "Light",
      "ShowWaveform": true,
      "WaveformPosition": "Bottom",
      "WaveformHeight": 100
    },
    "WhisperAPI": {
      "Model": "whisper-1",
      "BaseUrl": "https://api.openai.com/v1/audio/transcriptions",
      "Timeout": 30,
      "Language": "auto",
      "OutputMode": "transcription",
      "Temperature": 0.0,
      "ResponseFormat": "json"
    }
  }
}
```

#### 4.2 新增数据结构
```csharp
// 语言选项
public enum InputLanguage
{
    Auto,    // 自动检测
    Chinese, // zh
    English, // en
    // 可扩展更多语言
}

// 输出模式
public enum OutputMode
{
    Transcription, // 原语言转录
    Translation    // 翻译成英文
}

// 波形配置
public class WaveformConfig
{
    public bool Enabled { get; set; }
    public int Height { get; set; }
    public string ColorScheme { get; set; }
}
```

### 5. 安全性考虑

- API密钥继续使用Windows凭据管理器安全存储
- 代理密码同样使用安全存储
- URL验证防止注入攻击
- 输入参数验证和清理

### 6. 性能优化

- 波形渲染使用独立线程
- 音频数据处理优化，避免UI卡顿
- 懒加载设置窗口的选项卡内容
- 资源缓存机制

### 7. 兼容性保证

- 保持与现有配置文件的向后兼容
- 新功能的默认值不影响现有用户
- 渐进式UI更新，不破坏现有功能

## 技术栈总结

- **UI框架**: WPF + ModernWpfUI
- **动画**: WPF Storyboard + Custom Animations
- **音频可视化**: NAudio + Canvas + FFT
- **图标/图形**: 矢量图形（XAML Path）+ PNG图标
- **主题系统**: 资源字典 + 动态资源绑定
- **配置管理**: 扩展现有的ConfigManager
- **语言支持**: ISO-639-1标准语言代码

## 实施风险和缓解措施

1. **风险**: ModernWpfUI与现有代码冲突
   - **缓解**: 逐步迁移，先在新窗口测试

2. **风险**: 音频可视化影响性能
   - **缓解**: 提供性能模式选项，可降低刷新率

3. **风险**: 新UI在不同DPI设置下显示异常
   - **缓解**: 充分测试，使用DPI感知设置

4. **风险**: 用户不适应新界面
   - **缓解**: 保留经典模式选项