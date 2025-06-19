<#
.SYNOPSIS
    删除指定目录中所有存在硬链接的文件（智能去重版）

.DESCRIPTION
    此脚本会扫描指定目录及其子目录，使用 fsutil 命令查找所有存在硬链接的文件，
    并确保同一内容的文件只被处理一次，避免重复删除。

.PARAMETER Path
    要扫描的目录路径

.EXAMPLE
    .\Remove-HardlinkedFiles.ps1 -Path "C:\MyFolder"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path
)

# 记录开始时间
$startTime = Get-Date
Write-Host "脚本开始执行时间: $startTime" -ForegroundColor Cyan
Write-Host "使用 fsutil 检测硬链接..." -ForegroundColor Yellow

# 检查路径是否存在
if (-not (Test-Path -Path $Path -PathType Container)) {
    Write-Error "指定的路径不存在或不是目录: $Path"
    exit 1
}

# 检查 fsutil 是否可用
try {
    $null = fsutil
} catch {
    Write-Error "fsutil 命令不可用，请以管理员身份运行此脚本"
    exit 1
}

# 哈希表用于记录已处理的文件（基于文件唯一标识）
$processedFiles = @{}

Write-Host "正在扫描目录: $Path" -ForegroundColor Green
Write-Host "获取文件列表..." -ForegroundColor Yellow

# 获取所有文件
$files = Get-ChildItem -Path $Path -File -Recurse -ErrorAction SilentlyContinue

# 计数器
$totalFiles = $files.Count
$processed = 0
$hardlinkedFiles = 0
$skippedFiles = 0
$errorFiles = 0
$duplicateFilesSkipped = 0

Write-Host "共找到 $totalFiles 个文件" -ForegroundColor Green
Write-Host "开始分析文件..." -ForegroundColor Yellow

foreach ($file in $files) {
    $processed++
    $progress = [math]::Round(($processed / $totalFiles) * 100, 2)
    Write-Progress -Activity "正在处理文件" -Status "进度: $progress% ($processed/$totalFiles) - 当前文件: $($file.Name)" -PercentComplete $progress
    
    $filePath = $file.FullName
    
    # 获取文件唯一标识（使用文件内容和路径的组合作为标识）
    try {
        # 使用文件路径和大小作为临时标识（因为FileIndex可能不可用）
        $fileId = "$($file.Length)_$($filePath.ToLower())"
        
        # 尝试获取更精确的唯一标识（如果可用）
        try {
            $fileRef = Get-Item $filePath -Force
            if ($fileRef -ne $null -and $fileRef.FileIndex -ne $null) {
                $fileId = $fileRef.FileIndex.ToString()
            }
        } catch {
            # 如果无法获取FileIndex，继续使用路径和大小作为标识
        }
    } catch {
        $skippedFiles++
        Write-Warning "无法获取文件标识: $filePath (可能没有访问权限)"
        continue
    }
    
    # 检查是否已经处理过这个文件
    if ($fileId -ne $null -and $processedFiles.ContainsKey($fileId)) {
        $duplicateFilesSkipped++
        Write-Debug "跳过已处理的文件: $filePath (文件ID: $fileId)"
        continue
    }
    
    # 使用 fsutil 获取硬链接信息
    try {
        $fsutilOutput = fsutil hardlink list "$filePath" 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "fsutil 命令执行失败 (错误代码: $LASTEXITCODE)"
        }
        
        # 计算硬链接数量（输出行数减1，因为第一行是原文件路径）
        $hardlinks = ($fsutilOutput | Measure-Object -Line).Lines - 1
        
        if ($hardlinks -gt 1) {
            $hardlinkedFiles++
            Write-Host "发现硬链接文件 [$hardlinkedFiles]: $filePath" -ForegroundColor Magenta
            Write-Host "  ├─ 硬链接数: $hardlinks" -ForegroundColor DarkMagenta
            Write-Host "  ├─ 文件大小: $([math]::Round($file.Length/1KB, 2)) KB" -ForegroundColor DarkMagenta
            Write-Host "  ├─ 最后修改时间: $($file.LastWriteTime)" -ForegroundColor DarkMagenta
            
            # 显示所有硬链接位置（前3个）
            Write-Host "  ├─ 硬链接位置:" -ForegroundColor DarkMagenta
            $fsutilOutput | Select-Object -Skip 1 | Select-Object -First 3 | ForEach-Object {
                Write-Host "  │   ├─ $_" -ForegroundColor DarkGray
            }
            if ($hardlinks -gt 3) {
                Write-Host "  │   ├─ ...(还有 $($hardlinks - 3) 个)" -ForegroundColor DarkGray
            }
            
            # 记录所有硬链接文件标识，避免重复处理
            $fsutilOutput | Select-Object -Skip 1 | ForEach-Object {
                $linkedFilePath = $_
                try {
                    # 使用路径和大小作为基础标识
                    $linkedFileId = "$((Get-Item $linkedFilePath -Force).Length)_$($linkedFilePath.ToLower())"
                    
                    # 尝试获取更精确的标识
                    try {
                        $linkedFileRef = Get-Item $linkedFilePath -Force
                        if ($linkedFileRef -ne $null -and $linkedFileRef.FileIndex -ne $null) {
                            $linkedFileId = $linkedFileRef.FileIndex.ToString()
                        }
                    } catch {
                        # 如果无法获取FileIndex，继续使用路径和大小
                    }
                    
                    if (-not $processedFiles.ContainsKey($linkedFileId)) {
                        $processedFiles[$linkedFileId] = $true
                    }
                } catch {
                    Write-Debug "无法获取硬链接文件标识: $linkedFilePath"
                }
            }
            
            try {
                # 删除文件（不会影响其他硬链接）
                Write-Host "  └─ 正在删除文件..." -ForegroundColor DarkYellow
                Remove-Item -Path $filePath -Force -ErrorAction Stop
                Write-Host "     删除成功!" -ForegroundColor Green
            }
            catch {
                $errorFiles++
                Write-Host "     删除失败: $_" -ForegroundColor Red
            }
        }
        else {
            Write-Debug "普通文件: $filePath (硬链接数: 1)"
        }
    }
    catch {
        $skippedFiles++
        Write-Warning "无法检查文件 $filePath : $_"
        continue
    }
}

# 计算执行时间
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`n处理完成!" -ForegroundColor Cyan
Write-Host "执行摘要:" -ForegroundColor Yellow
Write-Host "├─ 开始时间: $startTime"
Write-Host "├─ 结束时间: $endTime"
Write-Host "├─ 总耗时: $($duration.TotalSeconds.ToString('0.00')) 秒"
Write-Host "├─ 扫描文件总数: $totalFiles"
Write-Host "├─ 发现硬链接文件组数: $hardlinkedFiles"
Write-Host "├─ 成功删除文件数: $($hardlinkedFiles - $errorFiles)"
Write-Host "├─ 删除失败文件数: $errorFiles"
Write-Host "├─ 跳过文件数: $skippedFiles"
Write-Host "└─ 避免重复处理的文件数: $duplicateFilesSkipped" -ForegroundColor Gray