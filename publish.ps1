# Publish Snaptic ra chỗ cố định và cập nhật đăng ký khởi động cùng Windows.
#
# Vì sao cần script này: bản build trong artifacts/ bị xoá mỗi lần dọn repo, mà khoá Run
# của Windows thì không biết điều đó — nó chỉ lặng lẽ không chạy được lúc đăng nhập, không
# báo lỗi gì. Publish ra %LOCALAPPDATA%\Programs\Snaptic là nằm ngoài repo, dọn build
# không đụng tới.
#
# Dùng:  .\publish.ps1

$ErrorActionPreference = 'Stop'

$dest = "$env:LOCALAPPDATA\Programs\Snaptic"
$exe  = "$dest\Snaptic.App.exe"
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Write-Host "Đích: $dest" -ForegroundColor Cyan

# App đang chạy thì giữ file, publish sẽ hỏng.
$running = Get-Process -Name 'Snaptic.App' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Đang tắt Snaptic ($($running.Count) tiến trình)..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# self-contained: app này khởi động cùng Windows, nên KHÔNG nên phụ thuộc .NET runtime
# trên máy. Gỡ hoặc nâng cấp .NET hỏng là nó im lặng không lên lúc đăng nhập, không báo gì.
# Đổi ~230MB đĩa lấy việc nó luôn chạy được là đáng.
#
# KHÔNG bật PublishTrimmed: Avalonia dùng reflection cho XAML, cắt nhầm là app chết lúc
# chạy theo kiểu rất khó truy. Đĩa rẻ hơn một app tray hỏng bí ẩn.
Write-Host "Đang publish (self-contained, Release)..." -ForegroundColor Cyan
dotnet publish src/Snaptic.App `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $dest

if ($LASTEXITCODE -ne 0) { throw "Publish thất bại (exit $LASTEXITCODE)" }
if (-not (Test-Path $exe)) { throw "Publish xong nhưng không thấy $exe" }

$size = [math]::Round((Get-ChildItem $dest -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB)
Write-Host "Publish xong: $size MB" -ForegroundColor Green

# Chỉ đụng registry khi đường dẫn thật sự khác — ghi lại y hệt là can thiệp vô ích.
$current = (Get-ItemProperty $runKey -ErrorAction SilentlyContinue).Snaptic
$wanted  = "`"$exe`""

if ($current -eq $wanted) {
    Write-Host "Khởi động cùng Windows: đã đúng đường dẫn, không đổi gì" -ForegroundColor Green
}
elseif ($null -eq $current) {
    Write-Host "Khởi động cùng Windows: đang TẮT — không tự bật lại." -ForegroundColor Yellow
    Write-Host "  Muốn bật thì vào Snaptic > Cài đặt > tick 'Khởi động cùng Windows'."
}
else {
    Set-ItemProperty $runKey -Name Snaptic -Value $wanted
    Write-Host "Đã cập nhật khởi động cùng Windows:" -ForegroundColor Green
    Write-Host "  cũ : $current"
    Write-Host "  mới: $wanted"
}

Write-Host ""
Write-Host "Chạy thử: $exe" -ForegroundColor Cyan
