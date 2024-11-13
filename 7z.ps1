# PowerShell版本压缩文件并计算SHA256哈希值

param(
    [string]$version
)

if ([string]::IsNullOrEmpty($version)) {
    Write-Error "Version argument is required."
    exit
}

$sourceDir = "src\EasyTidy\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64"
$targetDir = "src\EasyTidy\bin\x64\Release\net8.0-windows10.0.22621.0"

# 重命名目录
if (Test-Path -Path $sourceDir) {
    Rename-Item -Path $sourceDir -NewName "EasyTidy"
}

# 确保 publish 目录存在
if (!(Test-Path -Path ./publish)) {
    New-Item -ItemType Directory -Path ./publish
}

Copy-Item -Path ./run.bat -Destination ./publish/run.bat

Copy-Item -Path "$targetDir\EasyTidy" -Destination ./publish -Recurse

# 使用7-Zip创建ZIP文件
& 7z a -tzip "EasyTidy_${version}_win-x64.zip" ./publish/*

# 使用7-Zip创建7z文件
& 7z a -t7z "EasyTidy_${version}_win-x64_7z.7z" ./publish/*

# 使用7-Zip计算SHA256哈希值
& 7z h -scrcsha256 "EasyTidy_${version}_win-x64.zip" "EasyTidy_${version}_win-x64_7z.7z" | Out-File "EasyTidy_${version}_win-x64_sha256.txt"

Write-Host ""
Write-Host "========================================"
Write-Host "Compress the file and calculate the SHA256 hash successfully."
Write-Host "========================================"