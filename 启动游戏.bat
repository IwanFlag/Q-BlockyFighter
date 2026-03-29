@echo off
chcp 65001 >nul
title Q版方块人大乱斗
echo ================================
echo   Q版方块人大乱斗 v0.7.0
echo ================================
echo.
echo 正在启动游戏...
echo 游戏将在浏览器中打开
echo 关闭此窗口将结束游戏
echo.

cd /d "%~dp0..\Code"
start "" "index_local_v03_enhanced.html"

echo 游戏已启动！
echo.
echo 按任意键关闭此窗口...
pause >nul
