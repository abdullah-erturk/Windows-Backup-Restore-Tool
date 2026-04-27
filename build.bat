@echo off
title RecoveryTool Build
cd /d "%~dp0"

echo.
echo  [0/3] Killing locked processes...
taskkill /F /PID 12496 >nul 2>&1
taskkill /F /IM VBCSCompiler.exe /T >nul 2>&1
taskkill /F /IM MSBuild.exe /T >nul 2>&1
ping -n 2 127.0.0.1 >nul

echo  Cleaning obj...
if exist "RecoveryTool\obj" rmdir /s /q "RecoveryTool\obj"
if exist "build" rmdir /s /q "build"

echo  [1/3] Restoring packages...
dotnet restore RecoveryTool\RecoveryTool.csproj --runtime win-x64
if %ERRORLEVEL% neq 0 ( echo RESTORE FAILED & pause & exit /b 1 )

echo  [2/3] Publishing (skipping build step)...
dotnet publish RecoveryTool\RecoveryTool.csproj -c Release -r win-x64 ^
    -p:Platform=x64 ^
    -p:PublishSingleFile=true ^
    -p:SelfContained=true ^
    -p:PublishReadyToRun=false ^
    -p:EnableCompressionInSingleFile=true ^
    -p:Optimize=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    --output build\publish
if %ERRORLEVEL% neq 0 ( echo PUBLISH FAILED & pause & exit /b 1 )

echo.
echo  [3/3] Done!
for %%f in (build\publish\*.exe) do echo  Output: %%f  (%%~zf bytes)
echo.
pause
