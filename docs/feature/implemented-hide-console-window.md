# 隐藏控制台窗口
**状态**: implemented  
**创建时间**: 2025-01-21

## 用户故事
- 作为最终用户，我想在启动 SPEXT 时不看到控制台窗口，从而获得更专业、更干净的使用体验。

## 接受标准
- [x] Release 版本启动时不显示控制台窗口
- [x] Debug 版本保持控制台窗口显示
- [x] 应用程序正常运行，所有功能不受影响
- [x] 日志系统继续正常工作（输出到文件）
- [x] 系统托盘图标和设置窗口正常显示
- [x] 错误信息通过消息框显示

## 方案概览
- **技术思路**：修改项目文件的 OutputType 和 DisableWinExeOutputInference 设置
- **影响范围**：
  - VoiceInput.csproj 项目文件
  - 可能需要调整日志输出配置
  - 批处理启动脚本可能需要更新

## 实现细节
1. 修改 `VoiceInput/VoiceInput.csproj`：
   - 设置 `<OutputType>WinExe</OutputType>`
   - 确保 `<DisableWinExeOutputInference>true</DisableWinExeOutputInference>`

2. 验证日志系统：
   - 确认日志仍然输出到 `%LOCALAPPDATA%\SPEXT\Logs\`
   - 测试错误日志记录

3. 更新启动脚本：
   - 检查 `run.bat` 和 `cc.bat` 是否需要调整

## 估算与里程碑
| 阶段 | 预估工时 |
| ---- | -------- |
| 设计 | 0.5h |
| 开发 | 0.5h |
| 测试 | 0.5h |