@echo off
rem VishasController 배포 파일 만들기: publish\ 폴더에 실행 파일 2종을 만든다.
rem   self-contained\VishasController.exe : 런타임 포함(받는 PC에 아무것도 필요 없음)
rem   framework\VishasController.exe      : .NET 10 Desktop Runtime 필요
rem 환경 변수 VISHAS_CONTROLLER_BUILD_DIR이 있으면 그 폴더에도 복사한다.
rem 복사 위치는 PC마다 다르고 공개 저장소에 적지 않으므로 환경 변수로만 받는다.
rem 인자 nopause: 끝에 멈추지 않는다(Claude가 실행할 때). 더블클릭 실행은 그대로 멈춘다.
chcp 65001 >nul
cd /d "%~dp0"
set "NOPAUSE="
if /i "%~1"=="nopause" set "NOPAUSE=1"
set "PROJ=src\VishasController\VishasController.csproj"

echo [1/2] 런타임 포함판 만드는 중...
dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\self-contained -v q -nologo
if errorlevel 1 goto fail

echo [2/2] 런타임 별도판 만드는 중...
dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\framework -v q -nologo
if errorlevel 1 goto fail

echo 만들었습니다: publish\self-contained, publish\framework

if not defined VISHAS_CONTROLLER_BUILD_DIR (
    echo 환경 변수 VISHAS_CONTROLLER_BUILD_DIR이 없어 복사는 건너뜁니다.
    goto done
)

echo 복사 중: VISHAS_CONTROLLER_BUILD_DIR 폴더
if not exist "%VISHAS_CONTROLLER_BUILD_DIR%\self-contained\" mkdir "%VISHAS_CONTROLLER_BUILD_DIR%\self-contained"
if not exist "%VISHAS_CONTROLLER_BUILD_DIR%\framework\" mkdir "%VISHAS_CONTROLLER_BUILD_DIR%\framework"
copy /y "publish\self-contained\VishasController.exe" "%VISHAS_CONTROLLER_BUILD_DIR%\self-contained\" >nul
if errorlevel 1 goto copyfail
copy /y "publish\framework\VishasController.exe" "%VISHAS_CONTROLLER_BUILD_DIR%\framework\" >nul
if errorlevel 1 goto copyfail
echo 복사했습니다.
goto done

:copyfail
echo 복사 실패. 폴더에 접근할 수 있는지, 받는 쪽에서 앱이 실행 중이지 않은지 확인하세요.
if not defined NOPAUSE pause
exit /b 1

:fail
echo 빌드 실패. 위 메시지를 확인하세요.
if not defined NOPAUSE pause
exit /b 1

:done
if not defined NOPAUSE pause
exit /b 0
