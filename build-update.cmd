@echo off
setlocal

title GITmaxed - Build Update Checker

:: ========================================
:: Builds update.exe (C# WinForms, .NET Framework 4.8)
:: Output stays in this build-update folder.
:: ========================================

set "ROOT=%~dp0"
set "SRC=%ROOT%src\UpdateChecker.cs"
set "OUT=%ROOT%update.exe"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo ERROR: .NET Framework 4.8 compiler not found.
    exit /b 1
)

if not exist "%SRC%" (
    echo ERROR: source not found: %SRC%
    exit /b 1
)

echo Building update.exe ...
"%CSC%" /nologo /target:winexe /out:"%OUT%" ^
    /r:System.dll ^
    /r:System.Drawing.dll ^
    /r:System.Web.Extensions.dll ^
    /r:System.Windows.Forms.dll ^
    "%SRC%"

if not exist "%OUT%" (
    echo ERROR: build failed.
    exit /b 1
)

echo Done: %OUT%
exit /b 0