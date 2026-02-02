# =============================================
# Site Yönetim - Veritabanı Kurulum Scripti
# Yeni bilgisayarda veya "Invalid object name" hatası alındığında çalıştırın.
# =============================================

param(
    [string]$Server = ".",
    [string]$Database = "SiteYonetim",
    [string]$User = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScriptsDir = Join-Path $ScriptDir "Scripts"

# Tek script - tüm şema ve migration'lar
$scripts = @("Full-Schema.sql")

function Build-ConnectionString {
    if ($User -and $Password) {
        return "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;"
    }
    return "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
}

Write-Host "Site Yönetim - Veritabanı Kurulumu" -ForegroundColor Cyan
Write-Host "Server: $Server, Database: $Database" -ForegroundColor Gray
Write-Host ""

# appsettings.json'dan connection string oku (opsiyonel)
$appSettingsPath = Join-Path (Split-Path -Parent (Split-Path -Parent $ScriptDir)) "src\SiteYonetim.WebApi\appsettings.json"
if (Test-Path $appSettingsPath) {
    try {
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $connStr = $appSettings.ConnectionStrings.DefaultConnection
        if ($connStr) {
            if ($connStr -match "Server=([^;]+)") { $Server = $Matches[1] }
            if ($connStr -match "Database=([^;]+)") { $Database = $Matches[1] }
            if ($connStr -match "User Id=([^;]+)") { $User = $Matches[1] }
            if ($connStr -match "Password=([^;]+)") { $Password = $Matches[1] }
            Write-Host "appsettings.json'dan ayarlar okundu." -ForegroundColor Gray
        }
    } catch { }
}

$connStr = Build-ConnectionString

foreach ($scriptName in $scripts) {
    $scriptPath = Join-Path $ScriptsDir $scriptName
    if (-not (Test-Path $scriptPath)) {
        Write-Host "  [ATLA] $scriptName - dosya bulunamadı" -ForegroundColor Yellow
        continue
    }
    Write-Host "  Çalıştırılıyor: $scriptName ..." -ForegroundColor White -NoNewline
    try {
        if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
            $sqlcmdArgs = @("-S", $Server, "-d", "master", "-i", $scriptPath, "-b")
            if ($User -and $Password) {
                $sqlcmdArgs += @("-U", $User, "-P", $Password)
            } else {
                $sqlcmdArgs += "-E"
            }
            $result = & sqlcmd @sqlcmdArgs 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw ($result -join "`n")
            }
        } elseif (Get-Module -ListAvailable -Name SqlServer) {
            $params = @{ ServerInstance = $Server; Database = "master"; InputFile = $scriptPath; ErrorAction = "Stop" }
            if ($User -and $Password) { $params["Username"] = $User; $params["Password"] = $Password }
            Invoke-Sqlcmd @params
        } else {
            Write-Host ""
            Write-Host "HATA: sqlcmd veya SqlServer PowerShell modülü bulunamadı." -ForegroundColor Red
            Write-Host ""
            Write-Host "Manuel kurulum:" -ForegroundColor Yellow
            Write-Host "  1. SQL Server Management Studio'yu açın" -ForegroundColor White
            Write-Host "  2. $Server sunucusuna bağlanın" -ForegroundColor White
            Write-Host "  3. Scripts klasöründeki dosyaları SIRAYLA çalıştırın:" -ForegroundColor White
            foreach ($s in $scripts) {
                $p = Join-Path $ScriptsDir $s
                if (Test-Path $p) { Write-Host "     - $s" -ForegroundColor Gray }
            }
            exit 1
        }
        Write-Host " OK" -ForegroundColor Green
    } catch {
        Write-Host " HATA" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Veritabanı kurulumu tamamlandı." -ForegroundColor Green
Write-Host "Uygulamayı çalıştırabilirsiniz: dotnet run --project src/SiteYonetim.WebApi" -ForegroundColor Cyan
