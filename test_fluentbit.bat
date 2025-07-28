@echo off
echo ========================================
echo KIEM TRA FLUENT BIT
echo ========================================

echo.
echo 1. Kiem tra file fluent-bit.exe...
if exist "FluentBit\fluent-bit.exe" (
    echo [OK] Tim thay fluent-bit.exe
) else (
    echo [ERROR] Khong tim thay fluent-bit.exe
    pause
    exit /b 1
)

echo.
echo 2. Kiem tra file cau hinh...
if exist "fluent-bit.conf" (
    echo [OK] Tim thay fluent-bit.conf
    echo.
    echo Noi dung file cau hinh:
    echo ----------------------------------------
    type fluent-bit.conf
    echo ----------------------------------------
) else (
    echo [ERROR] Khong tim thay fluent-bit.conf
    pause
    exit /b 1
)

echo.
echo 3. Kiem tra process fluent-bit dang chay...
tasklist /FI "IMAGENAME eq fluent-bit.exe" 2>NUL | find /I /N "fluent-bit.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [INFO] Fluent Bit dang chay
    tasklist /FI "IMAGENAME eq fluent-bit.exe"
) else (
    echo [INFO] Fluent Bit chua chay
)

echo.
echo 4. Khoi dong Fluent Bit voi cua so CMD...
echo Dang khoi dong Fluent Bit...
start "Fluent Bit Console" "FluentBit\fluent-bit.exe" -c "fluent-bit.conf"

echo.
echo 5. Cho 3 giay de Fluent Bit khoi dong...
timeout /t 3 /nobreak > NUL

echo.
echo 6. Kiem tra lai process...
tasklist /FI "IMAGENAME eq fluent-bit.exe" 2>NUL | find /I /N "fluent-bit.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [SUCCESS] Fluent Bit da khoi dong thanh cong!
    tasklist /FI "IMAGENAME eq fluent-bit.exe"
) else (
    echo [ERROR] Fluent Bit khong the khoi dong
)

echo.
echo 7. Kiem tra thu muc logs...
if exist "logs" (
    echo [OK] Thu muc logs ton tai
    echo.
    echo Danh sach file log:
    dir logs\*.log 2>NUL
) else (
    echo [INFO] Thu muc logs chua ton tai
)

echo.
echo ========================================
echo HOAN TAT KIEM TRA
echo ========================================
echo.
echo Nhan phim bat ky de dong...
pause > NUL 