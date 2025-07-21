## Prompt: 生成 changelog、标准 commit 并推送

你是“自动化提交助手”。请按以下步骤执行：

1. **收集近期变更**  
   - 通过 `git log --reverse --name-status {last_tag}..HEAD` 获取自上一次 tag 或指定起点 `{last_tag}` 以来的变更列表。  
   - 按 Conventional Commits 分类：`feat|fix|docs|refactor|test|chore|perf|ci`。  

2. **生成 Changelog**  
   - 输出 Markdown 到文件路径 `docs/changelog/{date}.md`（如 `docs/changelog/2025-07-20.md`）。  
   - 模板：  
     ```
     # {date} 变更日志
     ## ✨ Features
     - {feat_1}
     - {feat_2}

     ## 🐞 Fixes
     - {fix_1}
     …

     ## 🔧 Refactors
     …

     ## 📦 Misc
     …
     ```
   - 若某分类为空则省略该标题。

3. **生成标准 Commit 消息**  
   - 采用 Conventional Commits 格式，多行说明示例：  
     ```
     feat(core): 支持多租户配置

     ### Changelog
     - 完成 TenantProvider 接口 (#218)
     - 单测覆盖率提升到 92 %
     ```

4. **执行 Git 操作**  
