# PowerShell版本压缩文件并计算SHA256哈希值

param(
    [string]$version
)

if ([string]::IsNullOrEmpty($version)) {
    Write-Error "Version argument is required."
    exit
}

Copy-Item -Path ./run.bat -Destination ./Output/run.bat

# 使用7-Zip创建ZIP文件
& 7z a -tzip "EasyTidy_${version}_win-x64.zip" ./Output/*

# 使用7-Zip创建7z文件
& 7z a -t7z "EasyTidy_${version}_win-x64_7z.7z" ./Output/*

# 使用7-Zip计算SHA256哈希值
& 7z h -scrcsha256 "EasyTidy_${version}_win-x64.zip" "EasyTidy_${version}_win-x64_7z.7z" | Out-File "EasyTidy_${version}_win-x64_sha256.txt"

Write-Host ""
Write-Host "========================================"
Write-Host "Compress the file and calculate the SHA256 hash successfully."
Write-Host "========================================"