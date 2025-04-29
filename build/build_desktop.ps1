<#
.SYNOPSIS
    发布 ArchiveMaster 应用程序到指定的目录，并支持 Windows、Linux 和 macOS。

.DESCRIPTION
    此脚本根据提供的参数发布 ArchiveMaster 应用程序。支持单文件发布，并根据参数发布自包含或不带运行时的版本。

.PARAMETER -w
    发布 Windows 版本 (win-x64)。

.PARAMETER -l
    发布 Linux 版本 (linux-x64)。

.PARAMETER -m
    发布 macOS 版本 (osx-x64)。

.PARAMETER -s
    发布自包含版本 (Self-contained)，将包含.NET运行时。

.PARAMETER --all
    发布所有平台的版本 (Windows, Linux, macOS)。

.EXAMPLE
    .\Publish.ps1 -w -s
    发布自包含的 Windows 版本。

.EXAMPLE
    .\Publish.ps1 --all
    发布所有平台的不带运行时的版本。

.EXAMPLE
    .\Publish.ps1 -l -m
    发布 Linux 和 macOS 版本，不带运行时。

.NOTES
    脚本需在支持.NET SDK 的环境下运行。

#>

param (
    [switch]$w,
    [switch]$l,
    [switch]$m,
    [switch]$s,
    [switch]$all
)

$rawDir = Get-Location

try {
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "未安装.NET SDK"
    }

    $publishDirectory = Read-Host "请输入发布目录（默认为Publish）"

    if ([string]::IsNullOrEmpty($publishDirectory)) {
        $publishDirectory = "Publish"
    }
    else {
        $publishDirectory = $publishDirectory.Trim('"')
    }

    Set-Location "$PSScriptRoot/.."

    if (Test-Path $publishDirectory -PathType Container) {
        Remove-Item -r $publishDirectory
    }

    New-Item -Path $publishDirectory -ItemType Directory | Out-Null
    
    $platforms = @()
    if ($w -or $all) { $platforms += "win-x64" }
    if ($l -or $all) { $platforms += "linux-x64" }
    if ($m -or $all) { $platforms += "osx-x64" }

    foreach ($platform in $platforms) {
        $selfContained = if ($s) { "--self-contained" } else { "--no-self-contained" }

        Write-Output "正在发布$platform"
        $outputDir = if ($s) { 
            Join-Path $publishDirectory "${platform}_sc" 
        }
        else { 
            Join-Path $publishDirectory $platform 
        }
        dotnet publish ArchiveMaster.UI.Desktop -r $platform -c Release -o $outputDir $selfContained /p:PublishSingleFile=true

        $outputFile = if ($platform -eq "win-x64") { "ArchiveMaster.exe" } else { "ArchiveMaster" }
        $sourceFile = if ($platform -eq "win-x64") { 
            Join-Path $outputDir "ArchiveMaster.UI.Desktop.exe" 
        }
        else { 
            Join-Path $outputDir "ArchiveMaster.UI.Desktop" 
        }
    
        $destinationFile = Join-Path $outputDir $outputFile
        Move-Item $sourceFile $destinationFile -Force
    }

    Write-Output "操作完成"
    Invoke-Item $publishDirectory
}
catch {
    Write-Error $_
}

Set-Location $rawDir

pause
