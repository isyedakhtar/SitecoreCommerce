ECHO OFF
CLS

PowerShell.exe -ExecutionPolicy Bypass -File .\build.ps1 -verbose	
IF ERRORLEVEL 1 GOTO errorHandling

PAUSE
EXIT /b 0

:errorHandling
PAUSE
EXIT /b -1
