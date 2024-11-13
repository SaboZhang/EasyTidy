:: by EasyTidy
@echo off
@ECHO OFF&(PUSHD "%~DP0")&(REG QUERY "HKU\S-1-5-19">NUL 2>&1)||(
powershell -Command "Start-Process '%~sdpnx0' -Verb RunAs"&&EXIT)

echo set WshShell = WScript.CreateObject("WScript.Shell")>tmp.vbs
echo set oShellLink = WshShell.CreateShortcut("%~dp0" ^& "\EasyTidy.lnk")>>tmp.vbs
echo oShellLink.TargetPath ="%~dp0EasyTidy\EasyTidy.exe">>tmp.vbs
echo oShellLink.WindowStyle ="1">>tmp.vbs
echo oShellLink.IconLocation = "%~dp0EasyTidy\EasyTidy.exe">>tmp.vbs
echo oShellLink.Description = "">>tmp.vbs
echo oShellLink.WorkingDirectory = "%~dp0">>tmp.vbs
echo oShellLink.Save>>tmp.vbs
call tmp.vbs
del /f /q tmp.vbs

start "" "%~dp0EasyTidy\EasyTidy.exe"
