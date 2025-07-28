param(
    [string]$LogType = "winlog"
)

Write-Host "=== KIEM TRA FILE LOG: $LogType ===" -ForegroundColor Green

$logFile = "logs\$LogType.log"

if (-not (Test-Path $logFile)) {
    Write-Host "File $logFile khong ton tai!" -ForegroundColor Red
    exit 1
}

$fileInfo = Get-Item $logFile
Write-Host "File: $logFile" -ForegroundColor Yellow
Write-Host "Kich thuoc: $($fileInfo.Length) bytes" -ForegroundColor Yellow
Write-Host "Tao luc: $($fileInfo.CreationTime)" -ForegroundColor Yellow
Write-Host "Sua luc: $($fileInfo.LastWriteTime)" -ForegroundColor Yellow

# Doc toan bo file
$content = Get-Content $logFile -Raw
$lines = Get-Content $logFile
Write-Host "Tong so dong: $($lines.Count)" -ForegroundColor Yellow

# Kiem tra encoding va BOM
$bytes = [System.IO.File]::ReadAllBytes($logFile)
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    Write-Host "File co UTF-8 BOM" -ForegroundColor Yellow
} else {
    Write-Host "File khong co BOM" -ForegroundColor Green
}

Write-Host "`n=== PHAN TICH DONG DAU TIEN ===" -ForegroundColor Green
# Hien thi 5 dong dau
for ($i = 0; $i -lt [Math]::Min(5, $lines.Count); $i++) {
    Write-Host "Dong $($i+1): $($lines[$i])" -ForegroundColor Cyan
}

Write-Host "`n=== PHAN TICH DONG CUOI ===" -ForegroundColor Green
# Hien thi 5 dong cuoi
for ($i = [Math]::Max(0, $lines.Count - 5); $i -lt $lines.Count; $i++) {
    Write-Host "Dong $($i+1): $($lines[$i])" -ForegroundColor Cyan
}

Write-Host "`n=== KIEM TRA JSON FRAGMENTS ===" -ForegroundColor Green
$fragmentCount = 0
$validJsonCount = 0
$invalidJsonCount = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i].Trim()
    if ([string]::IsNullOrEmpty($line)) { continue }
    
    # Kiem tra JSON fragment
    if (-not $line.StartsWith('{') -or -not $line.EndsWith('}')) {
        $fragmentCount++
        if ($fragmentCount -le 3) {
            Write-Host "Fragment $fragmentCount (dong $($i+1)): $line" -ForegroundColor Red
        }
    } else {
        # Thu parse JSON
        try {
            $jsonObj = $line | ConvertFrom-Json
            $validJsonCount++
        } catch {
            $invalidJsonCount++
            if ($invalidJsonCount -le 3) {
                Write-Host "Invalid JSON $invalidJsonCount (dong $($i+1)): $($_.Exception.Message)" -ForegroundColor Red
                Write-Host "   Content: $line" -ForegroundColor Red
            }
        }
    }
}

Write-Host "Thong ke:" -ForegroundColor Yellow
Write-Host "   Valid JSON: $validJsonCount" -ForegroundColor Green
Write-Host "   Invalid JSON: $invalidJsonCount" -ForegroundColor Red
Write-Host "   JSON Fragments: $fragmentCount" -ForegroundColor Yellow

# Tim kiem pattern cu the ma user bao cao
Write-Host "`n=== TIM KIEM PATTERN LOI ===" -ForegroundColor Green
$errorPatterns = @(
    ',"Message":',
    ',"StringInserts":[]}',
    ',"Message":"Offline downlevel migration succeeded'
)

foreach ($pattern in $errorPatterns) {
    $matches = $content | Select-String $pattern -AllMatches
    if ($matches) {
        Write-Host "Tim thay pattern '$pattern': $($matches.Count) lan" -ForegroundColor Red
        foreach ($match in $matches | Select-Object -First 3) {
            $lineNum = ($content.Substring(0, $match.Index) -split "`r?`n").Count
            Write-Host "   Dong $lineNum`: $($match.Line.Trim())" -ForegroundColor Red
        }
    } else {
        Write-Host "Khong tim thay pattern '$pattern'" -ForegroundColor Green
    }
}

# Kiem tra cau truc JSON cua 10 dong dau
Write-Host "`n=== KIEM TRA CAU TRUC JSON (10 dong dau) ===" -ForegroundColor Green
for ($i = 0; $i -lt [Math]::Min(10, $lines.Count); $i++) {
    $line = $lines[$i].Trim()
    if ([string]::IsNullOrEmpty($line)) { continue }
    
    try {
        $jsonObj = $line | ConvertFrom-Json
        if ($LogType -eq "winlog") {
            $timeGen = $jsonObj.TimeGenerated
            $source = $jsonObj.SourceName
            $eventId = $jsonObj.EventID
            Write-Host "Dong $($i+1): TimeGenerated=$timeGen, Source=$source, EventID=$eventId" -ForegroundColor Green
        } else {
            Write-Host "Dong $($i+1): Parse thanh cong" -ForegroundColor Green
        }
    } catch {
        Write-Host "Dong $($i+1): $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Content: $line" -ForegroundColor Red
    }
}

Write-Host "`n=== HOAN THANH KIEM TRA ===" -ForegroundColor Green 