# 测试API设置保存和加载功能的脚本
Write-Host "正在测试API设置保存功能..." -ForegroundColor Green

# 1. 备份当前配置
$configPath = "$env:LOCALAPPDATA\VoiceInput\appsettings.json"
if (Test-Path $configPath) {
    Copy-Item $configPath "$configPath.backup" -Force
    Write-Host "已备份配置文件"
}

# 2. 运行应用程序并等待
Write-Host "`n请按以下步骤测试："
Write-Host "1. 打开设置窗口"
Write-Host "2. 在基本设置中输入API密钥"
Write-Host "3. 切换到API设置"
Write-Host "4. 修改语言设置（如选择'中文'）"
Write-Host "5. 修改Temperature值（如0.5）"
Write-Host "6. 点击保存"
Write-Host "7. 重新打开设置窗口，检查："
Write-Host "   - API密钥是否还在"
Write-Host "   - 语言设置是否保存"
Write-Host "   - Temperature值是否保存"

# 3. 运行应用
cd VoiceInput
dotnet run

# 4. 检查配置文件的更改
if (Test-Path $configPath) {
    Write-Host "`n配置文件内容：" -ForegroundColor Yellow
    Get-Content $configPath | ConvertFrom-Json | ConvertTo-Json -Depth 10
}

Write-Host "`n测试完成" -ForegroundColor Green