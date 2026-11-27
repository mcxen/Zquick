@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [错误] 未检测到 .NET 8 SDK。请先安装 .NET 8 SDK：
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

if /i "%~1"=="standalone" (
    echo 正在编译免运行库独立单文件版本 ^(win-x64 self-contained^)...
    dotnet publish Zquick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\standalone
    if errorlevel 1 (
        echo [失败] 编译出错。
        pause
        exit /b 1
    )
    echo.
    echo [成功] 已输出到 publish\standalone\Zquick.exe
) else if /i "%~1"=="installer" (
    echo 正在编译独立单文件版本并生成 Inno Setup 安装包...
    dotnet publish Zquick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\standalone
    if errorlevel 1 (
        echo [失败] .NET 发布出错。
        pause
        exit /b 1
    )

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
    echo.
    echo [成功] 安装包已输出到 dist\ 目录。
) else (
    echo 正在编译轻量单文件版本 ^(依赖本机 .NET 8 Desktop Runtime^)...
    echo 提示：可用 "build.bat standalone" 生成免运行库版，或 "build.bat installer" 生成安装包
    dotnet publish Zquick.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\light
    if errorlevel 1 (
        echo [失败] 编译出错。
        pause
        exit /b 1
    )
    echo.
    echo [成功] 已输出到 publish\light\Zquick.exe
)

pause
