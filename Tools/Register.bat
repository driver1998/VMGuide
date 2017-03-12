@echo off
cd /d %~dp0
if not exist VMGuide.exe goto error
echo.
echo VMGuide 3.0 Temporary Installer
echo This program will register the URL Protocol vm-settings to your user profile.
echo.
pause
goto register

:error
echo.
echo VMGuide.exe is missing£¡
pause
exit


:register
>nul reg add HKCU\Software\Classes\vm-settings 			/f
>nul reg add HKCU\Software\Classes\vm-settings 			/f /v "URL Protocol"
>nul reg add HKCU\Software\Classes\vm-settings\shell	 		/f
>nul reg add HKCU\Software\Classes\vm-settings\shell\open 		/f
>nul reg add HKCU\Software\Classes\vm-settings\shell\open\command 	/f
>nul reg add HKCU\Software\Classes\vm-settings\shell\open\command	/f /ve /d """"%~dp0VMGuide.exe""" %%1"
echo.
echo Registration complete.
echo Now you can use an URL like...
echo. 
echo 	vm-settings://type/value
echo.
echo ... to import settings to your Virtual Machine.
echo.
echo Examples:
echo.
echo 	vm-settings://biosdate/19991231
echo 	vm-settings://datelock/false
echo 	vm-settings://acpi/false
echo.
pause
exit