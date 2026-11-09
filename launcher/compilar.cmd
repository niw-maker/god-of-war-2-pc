@echo off
rem Compila o launcher com o compilador do .NET Framework que ja vem no Windows.
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0..\GoW2 Launcher.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "%~dp0GoW2Launcher.cs"
exit /b %ERRORLEVEL%
