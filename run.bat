@echo off
setlocal

:: 设置 EasyTidy 目录
set "easyTidyDir=%cd%\EasyTidy"

:: 检查 EasyTidy 目录是否存在
if not exist "%easyTidyDir%" (
    echo EasyTidy directory not found!
    exit /b 1
)

:: 检查 EasyTidy.exe 是否存在
if not exist "%easyTidyDir%\EasyTidy.exe" (
    echo EasyTidy.exe not found in EasyTidy directory!
    exit /b 1
)

:: 运行 EasyTidy.exe
echo Running EasyTidy.exe...
"%easyTidyDir%\EasyTidy.exe"

endlocal
