@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [错误] 未检测到 .NET SDK。请先安装 .NET 8 SDK：
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo 正在编译 Zquick ^(net48 Release，~150 KB 零依赖单文件^)...
dotnet build Zquick.csproj -c Release -o publish
if errorlevel 1 (
    echo [失败] 编译出错。
    pause
    exit /b 1
)
echo [成功] 已输出到 publish\Zquick.exe

if /i "%~1"=="installer" (
    set "ISCC_PATH="
    if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC_PATH=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    if not defined ISCC_PATH if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC_PATH=%ProgramFiles%\Inno Setup 6\ISCC.exe"

    if not defined ISCC_PATH (
        echo [错误] 未找到 Inno Setup 6 ^(ISCC.exe^)，请先安装 Inno Setup 6。
        pause
        exit /b 1
    )

    if not exist dist mkdir dist
    "%ISCC_PATH%" installer\Zquick.iss
    if errorlevel 1 (
        echo [失败] 安装包打包出错。
        pause
        exit /b 1
    )
    echo [成功] 安装包已输出到 dist\ 目录。
)

pause
