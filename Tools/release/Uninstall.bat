@echo off
setlocal EnableExtensions
title Subnautica Below Zero Multiplayer (LAN) - Uninstall

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%ProgramFiles(x86)%\Steam\steamapps\common\SubnauticaZero"

:checkgame
if exist "%GAME%\SubnauticaZero.exe" goto found
echo.
echo Could not find SubnauticaZero.exe in:
echo   %GAME%
set "GAME="
set /p "GAME=Type or paste your Subnautica Below Zero folder: "
set "GAME=%GAME:"=%"
if "%GAME%"=="" goto fail
goto checkgame

:found
set "ASM=%GAME%\SubnauticaZero_Data\Managed\Assembly-CSharp.dll"
if not exist "%ASM%.original" (
    echo No backup found. Use "Verify integrity of game files" in Steam to restore the original.
    goto fail
)
copy /y "%ASM%.original" "%ASM%" >nul
del "%ASM%.original"
echo.
echo Original Assembly-CSharp.dll restored.
echo The "Multiplayer" folder in the game folder was left in place (it holds your multiplayer saves). Delete it by hand if you want.
echo.
pause
exit /b 0

:fail
echo.
pause
exit /b 1
