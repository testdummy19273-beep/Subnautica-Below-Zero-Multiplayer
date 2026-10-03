@echo off
setlocal EnableExtensions
title Subnautica Below Zero Multiplayer (LAN) - Install

rem Usage: Install.bat ["C:\path\to\SubnauticaZero"]
set "GAME=%~1"
if "%GAME%"=="" set "GAME=%ProgramFiles(x86)%\Steam\steamapps\common\SubnauticaZero"

:checkgame
if exist "%GAME%\SubnauticaZero.exe" goto found
echo.
echo Could not find SubnauticaZero.exe in:
echo   %GAME%
set "GAME="
set /p "GAME=Type or paste your Subnautica Below Zero folder (the one with SubnauticaZero.exe): "
set "GAME=%GAME:"=%"
if "%GAME%"=="" goto fail
goto checkgame

:found
set "MANAGED=%GAME%\SubnauticaZero_Data\Managed"
set "ASM=%MANAGED%\Assembly-CSharp.dll"
set "HERE=%~dp0"
echo.
echo Game folder: %GAME%

rem Back up the untouched game file. If the file is already patched (and a backup exists) keep the backup.
findstr /m /c:"SubnauticaBootstrap" "%ASM%" >nul 2>&1
if errorlevel 1 (
    copy /y "%ASM%" "%ASM%.original" >nul
    echo Backed up Assembly-CSharp.dll to Assembly-CSharp.dll.original
) else (
    if not exist "%ASM%.original" (
        echo.
        echo Assembly-CSharp.dll is already patched but no backup exists.
        echo Verify game files in Steam first, then run this installer again.
        goto fail
    )
)

"%HERE%Patcher\gametool.exe" inject "%ASM%.original" "%HERE%Patcher\SubnauticaBootstrap.dll" "%ASM%.patched" "%MANAGED%"
if errorlevel 1 goto fail
move /y "%ASM%.patched" "%ASM%" >nul

xcopy "%HERE%Multiplayer" "%GAME%\Multiplayer" /e /i /y /q >nul
if errorlevel 1 goto fail
if not exist "%GAME%\Multiplayer\Game\Logs" mkdir "%GAME%\Multiplayer\Game\Logs"
if not exist "%GAME%\Multiplayer\Game\Plugins" mkdir "%GAME%\Multiplayer\Game\Plugins"
if not exist "%GAME%\Multiplayer\Game\Saves" mkdir "%GAME%\Multiplayer\Game\Saves"

echo.
echo Done. Start the game once, close it, then see README-LAN.txt for the Config.json options.
echo If Steam updates the game, run Install.bat again.
echo.
pause
exit /b 0

:fail
echo.
echo Install failed.
echo.
pause
exit /b 1
