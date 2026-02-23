param()

$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = (Resolve-Path (Join-Path $ScriptRoot "..")).Path
$PidDir = Join-Path $Root ".medicsys-pids"
$DockerMarker = Join-Path $PidDir "postgres.docker"
$ApiDockerMarker = Join-Path $PidDir "api.docker"

function Write-Info([string]$Message) {
    Write-Host "[MEDICSYS] $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "[MEDICSYS] $Message" -ForegroundColor Green
}

function Stop-FromPidFile([string]$PidFileName, [string]$Label) {
    $pidPath = Join-Path $PidDir $PidFileName
    if (-not (Test-Path $pidPath)) {
        return
    }

    $rawPid = (Get-Content $pidPath -ErrorAction SilentlyContinue | Select-Object -First 1).Trim()
    $parsedPid = 0
    if ([int]::TryParse($rawPid, [ref]$parsedPid)) {
        $process = Get-Process -Id $parsedPid -ErrorAction SilentlyContinue
        if ($process) {
            Stop-Process -Id $parsedPid -Force -ErrorAction SilentlyContinue
            Write-Info "$Label detenido (PID $parsedPid)"
        }
    }

    Remove-Item $pidPath -Force -ErrorAction SilentlyContinue
}

function Stop-ByCommandLine([string]$ProcessName, [string]$MatchText, [string]$Label) {
    $processes = Get-CimInstance Win32_Process -Filter "Name = '$ProcessName'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like "*$MatchText*" }

    foreach ($proc in $processes) {
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Info "$Label detenido (PID $($proc.ProcessId))"
    }
}

if (-not (Test-Path $PidDir)) {
    New-Item -Path $PidDir -ItemType Directory | Out-Null
}

Stop-FromPidFile -PidFileName "backend.pid" -Label "Backend"
Stop-FromPidFile -PidFileName "frontend.pid" -Label "Frontend"
Stop-FromPidFile -PidFileName "browser.pid" -Label "Browser"

# Limpieza extra por si quedaron procesos sin PID guardado.
Stop-ByCommandLine -ProcessName "dotnet.exe" -MatchText "MEDICSYS.Api" -Label "Backend residual"
Stop-ByCommandLine -ProcessName "node.exe" -MatchText "MEDICSYS.Web" -Label "Frontend residual"

if (Test-Path $DockerMarker) {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if ($dockerCommand) {
        Write-Info "Deteniendo postgres iniciado por script..."
        Push-Location $Root
        try {
            docker compose stop postgres | Out-Host
        }
        finally {
            Pop-Location
        }
    }

    Remove-Item $DockerMarker -Force -ErrorAction SilentlyContinue
}

if (Test-Path $ApiDockerMarker) {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if ($dockerCommand) {
        Write-Info "Deteniendo API iniciada por script..."
        Push-Location $Root
        try {
            docker compose stop api | Out-Host
        }
        finally {
            Pop-Location
        }
    }

    Remove-Item $ApiDockerMarker -Force -ErrorAction SilentlyContinue
}

Write-Ok "Detencion completada."
