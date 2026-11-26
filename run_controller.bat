@echo off
rem VishasController 실행: Release로 빌드한 뒤 트레이 앱을 띄운다.
rem Release 폴더를 쓰므로 앱이 켜져 있어도 개발용(Debug) 빌드와 겹치지 않는다.
chcp 65001 >nul
cd /d "%~dp0"

tasklist /fi "imagename eq VishasController.exe" | find /i "VishasController.exe" >nul
if not errorlevel 1 (
    echo VishasController가 이미 실행 중입니다. 트레이에서 종료한 뒤 다시 실행하세요.
    pause
    exit /b 1
)

echo 빌드 중...
dotnet build src\VishasController\VishasController.csproj -c Release -v q -nologo
if errorlevel 1 (
    echo 빌드 실패. 위 메시지를 확인하세요.
    pause
    exit /b 1
)

start "" "src\VishasController\bin\Release\net10.0-windows\VishasController.exe"
echo 실행했습니다. 작업 표시줄 오른쪽 트레이를 확인하세요.
timeout /t 3 >nul
