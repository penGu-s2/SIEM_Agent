# Script kiểm tra file log Fluent Bit
param(
    [string]$LogType = "winlog"
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "KIEM TRA FILE LOG FLUENT BIT" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

$logFile = "logs\$LogType.log"

Write-Host "`n1. Kiểm tra file log: $logFile" -ForegroundColor Yellow

if (Test-Path $logFile) {
    Write-Host "[OK] Tìm thấy file log" -ForegroundColor Green
    
    $fileInfo = Get-Item $logFile
    Write-Host "Kích thước: $($fileInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host "Ngày tạo: $($fileInfo.CreationTime)" -ForegroundColor Cyan
    Write-Host "Ngày sửa đổi: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
    
    $lines = Get-Content $logFile
    Write-Host "Số dòng: $($lines.Count)" -ForegroundColor Cyan
    
    if ($lines.Count -gt 0) {
        Write-Host "`n2. Dòng đầu tiên:" -ForegroundColor Yellow
        Write-Host $lines[0] -ForegroundColor White
        
        Write-Host "`n3. Dòng cuối cùng:" -ForegroundColor Yellow
        Write-Host $lines[-1] -ForegroundColor White
        
        Write-Host "`n4. Thử parse JSON từ dòng đầu tiên:" -ForegroundColor Yellow
        try {
            $firstLog = $lines[0] | ConvertFrom-Json
            Write-Host "Parse thành công!" -ForegroundColor Green
            Write-Host "TimeGenerated: $($firstLog.TimeGenerated)" -ForegroundColor Cyan
            Write-Host "SourceName: $($firstLog.SourceName)" -ForegroundColor Cyan
            Write-Host "EventID: $($firstLog.EventID)" -ForegroundColor Cyan
            Write-Host "Message: $($firstLog.Message)" -ForegroundColor Cyan
        }
        catch {
            Write-Host "Lỗi parse JSON: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host "`n5. Kiểm tra 5 dòng đầu:" -ForegroundColor Yellow
        for ($i = 0; $i -lt [Math]::Min(5, $lines.Count); $i++) {
            Write-Host "Dòng $($i + 1): $($lines[$i])" -ForegroundColor White
        }
    }
    else {
        Write-Host "[WARNING] File log rỗng" -ForegroundColor Yellow
    }
}
else {
    Write-Host "[ERROR] Không tìm thấy file log" -ForegroundColor Red
}

Write-Host "`n6. Kiểm tra thư mục logs:" -ForegroundColor Yellow
if (Test-Path "logs") {
    $logFiles = Get-ChildItem "logs\*.log"
    Write-Host "Các file log tìm thấy:" -ForegroundColor Cyan
    foreach ($file in $logFiles) {
        Write-Host "  - $($file.Name) ($($file.Length) bytes)" -ForegroundColor White
    }
}
else {
    Write-Host "[ERROR] Không tìm thấy thư mục logs" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "HOAN TAT KIEM TRA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green 