# 版本更新提醒功能
**状态**: pending-confirm  
**创建时间**: 2025-01-21 10:30:00

## 用户故事
- 作为 SPEXT 用户，我想在启动应用时检查是否有新版本，并在有更新时收到提醒，从而了解最新版本信息。

## 接受标准
- [ ] 应用启动时自动检查最新版本
- [ ] 发现新版本时显示更新提示对话框
- [ ] 显示新版本号和更新内容
- [ ] 提供「前往下载」按钮，点击后打开 GitHub Release 页面
- [ ] 提供「稍后提醒」选项
- [ ] 提供「跳过此版本」选项
- [ ] 记住用户选择，避免重复提醒同一版本

## 方案概览
- **技术思路**：
  - 使用 GitHub API 获取最新 Release 信息
  - 比较版本号判断是否有新版本
  - 显示更新提醒对话框
  - 点击下载按钮时打开默认浏览器访问 Release 页面
  - 本地存储用户的跳过选择

- **影响范围**：
  - 新增 UpdateCheckService 服务
  - 新增更新提醒对话框
  - 修改 App.xaml.cs 启动流程
  - 配置文件增加更新检查相关设置

## 估算与里程碑
| 阶段 | 预估工时 |
| ---- | -------- |
| 设计 | 0.5 d |
| 开发 | 1.5 d |
| 测试 | 0.5 d |

## 技术细节
- **版本管理**：使用语义化版本号（Major.Minor.Patch）
- **GitHub API**：
  - 接口：`https://api.github.com/repos/{owner}/{repo}/releases/latest`
  - 获取：版本号、发布日期、更新说明、下载链接
- **用户偏好存储**：
  - 跳过的版本列表
  - 上次检查时间
  - 检查频率设置（可选）
- **更新提醒流程**：
  1. 应用启动时异步检查更新
  2. 比较当前版本与最新版本
  3. 检查是否已跳过该版本
  4. 显示更新提醒对话框
  5. 根据用户选择执行相应操作