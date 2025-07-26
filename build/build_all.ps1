Write-Host "正在构建Linux非自包含版本..." -ForegroundColor Cyan
./build/build_linux64.ps1

Write-Host "正在构建Linux自包含版本..." -ForegroundColor Cyan
./build/build_linux64_sc.ps1

Write-Host "正在构建macOS非自包含版本..." -ForegroundColor Cyan
./build/build_macos.ps1

Write-Host "正在构建macOS自包含版本..." -ForegroundColor Cyan
./build/build_macos_sc.ps1

Write-Host "正在构建Windows非自包含版本..." -ForegroundColor Cyan
./build/build_win64.ps1

Write-Host "正在构建Windows自包含版本..." -ForegroundColor Cyan
./build/build_win64_sc.ps1

Write-Host "所有平台构建完成！" -ForegroundColor Green