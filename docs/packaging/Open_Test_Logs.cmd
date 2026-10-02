@echo off
set "HAIRBALL_LOGS=%APPDATA%\Godot\app_userdata\Project Hairball"
if not exist "%HAIRBALL_LOGS%" mkdir "%HAIRBALL_LOGS%"
start "" explorer.exe "%HAIRBALL_LOGS%"
