@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

echo.
echo ╔══════════════════════════════════════╗
echo ║        LOTW Table Converter          ║
echo ╚══════════════════════════════════════╝
echo.

:: ── Python 존재 여부 확인 ──────────────────────────────────────
where python >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [오류] Python 이 설치되어 있지 않거나 PATH 에 등록되지 않았습니다.
    echo.
    echo  Python 3.x 를 설치하세요: https://www.python.org/downloads/
    echo  설치 시 "Add Python to PATH" 옵션을 반드시 체크하세요.
    echo.
    pause
    exit /b 1
)

:: ── 변환 실행 ─────────────────────────────────────────────────
:: 인자 없이 실행하면 Excel/ 내 모든 .xlsx, .xml 변환
:: 테이블 이름을 인자로 넘기면 해당 테이블만 변환
::   예) ConvertTable.bat Dialogue
if "%~1"=="" (
    echo [전체 변환] Excel 폴더의 .xlsx, .xml 을 모두 변환합니다.
) else (
    echo [단일 변환] %~1 테이블만 변환합니다.
)
echo.

python convert_table.py %~1
set RESULT=%ERRORLEVEL%

echo.
if %RESULT% EQU 0 (
    echo ✓ 변환 완료! Unity 에디터에서 Refresh (Ctrl+R) 를 눌러주세요.
) else (
    echo ✗ 변환 중 오류가 발생했습니다. 위 메시지를 확인하세요.
)
echo.
pause
endlocal
exit /b %RESULT%
