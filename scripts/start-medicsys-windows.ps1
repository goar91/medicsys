param()

$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = (Resolve-Path (Join-Path $ScriptRoot "..")).Path
$PidDir = Join-Path $Root ".medicsys-pids"
$EnvFile = Join-Path $Root ".env"
$DockerMarker = Join-Path $PidDir "postgres.docker"
$ApiDockerMarker = Join-Path $PidDir "api.docker"

function Write-Info([string]$Message) {
    Write-Host "[MEDICSYS] $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "[MEDICSYS] $Message" -ForegroundColor Green
}

function Write-Warn([string]$Message) {
    Write-Host "[MEDICSYS] $Message" -ForegroundColor Yellow
}

function Parse-ConnectionString([string]$ConnectionString) {
    $result = @{}
    foreach ($segment in ($ConnectionString -split ";")) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $parts = $segment -split "=", 2
        if ($parts.Length -eq 2) {
            $result[$parts[0].Trim().ToLowerInvariant()] = $parts[1].Trim()
        }
    }

    return $result
}

function Set-ConnectionPort([string]$ConnectionString, [int]$Port) {
    $parts = @()
    $hasPort = $false
    foreach ($segment in ($ConnectionString -split ";")) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        if ($segment -match "^\s*Port\s*=") {
            $parts += "Port=$Port"
            $hasPort = $true
        }
        else {
            $parts += $segment.Trim()
        }
    }

    if (-not $hasPort) {
        $parts += "Port=$Port"
    }

    return ($parts -join ";")
}

function Test-TcpPort([string]$HostName, [int]$Port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(3000, $false)) {
            $client.Close()
            return $false
        }

        $client.EndConnect($async)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Wait-HttpOk(
    [string]$Url,
    [int]$MaxAttempts = 45,
    [int]$DelaySeconds = 1
) {
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return $true
            }
        }
        catch {
            # Reintentar hasta agotar intentos
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    return $false
}

function Load-DotEnv([string]$FilePath) {
    if (-not (Test-Path $FilePath)) {
        throw "No se encontro el archivo .env en $FilePath"
    }

    foreach ($raw in Get-Content $FilePath) {
        $line = $raw.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        $parts = $line -split "=", 2
        if ($parts.Length -ne 2) {
            continue
        }

        [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
    }
}

function Stop-TrackedProcess([string]$PidFileName) {
    $pidPath = Join-Path $PidDir $PidFileName
    if (-not (Test-Path $pidPath)) {
        return
    }

    $rawPid = (Get-Content $pidPath -ErrorAction SilentlyContinue | Select-Object -First 1).Trim()
    $parsedPid = 0
    if (-not [int]::TryParse($rawPid, [ref]$parsedPid)) {
        Remove-Item $pidPath -Force -ErrorAction SilentlyContinue
        return
    }

    $pidValue = $parsedPid
    $process = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $pidValue -Force -ErrorAction SilentlyContinue
    }

    Remove-Item $pidPath -Force -ErrorAction SilentlyContinue
}

function Ensure-RequiredEnv() {
    $required = @(
        "ConnectionStrings__DefaultConnection",
        "ConnectionStrings__OdontologiaConnection",
        "ConnectionStrings__AcademicoConnection",
        "Jwt__Key"
    )

    $missing = @()
    foreach ($name in $required) {
        $value = [Environment]::GetEnvironmentVariable($name, "Process")
        if ([string]::IsNullOrWhiteSpace($value)) {
            $missing += $name
        }
    }

    if ($missing.Count -gt 0) {
        throw ("Faltan variables de entorno en .env: " + ($missing -join ", "))
    }
}

function Ensure-DatabasesWithPsql(
    [string]$HostName,
    [int]$Port,
    [string]$User,
    [string]$Password,
    [string[]]$Databases
) {
    $psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
    if (-not $psqlCommand) {
        return $false
    }

    $env:PGPASSWORD = $Password
    try {
        foreach ($db in $Databases) {
            $dbEscaped = $db.Replace("'", "''")
            $exists = & $psqlCommand.Source -h $HostName -p $Port -U $User -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$dbEscaped';"
            if ($LASTEXITCODE -ne 0) {
                throw "No se pudo consultar la base '$db' con psql."
            }

            if (($exists | Out-String).Trim() -ne "1") {
                $quotedDb = '"' + $db.Replace('"', '""') + '"'
                & $psqlCommand.Source -h $HostName -p $Port -U $User -d postgres -c "CREATE DATABASE $quotedDb;"
                if ($LASTEXITCODE -ne 0) {
                    throw "No se pudo crear la base '$db' con psql."
                }
                Write-Info "Base creada: $db"
            }
            else {
                Write-Info "Base verificada: $db"
            }
        }
    }
    finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }

    return $true
}

function Ensure-DatabasesWithDocker([string]$User, [string[]]$Databases) {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCommand) {
        throw "No se encontro psql local ni docker para verificar/crear bases."
    }

    $containerName = "medicsys-postgres"
    $runningNames = docker ps --format "{{.Names}}"
    if (-not (($runningNames -split "`r?`n") -contains $containerName)) {
        Push-Location $Root
        try {
            docker compose up -d postgres | Out-Host
        }
        finally {
            Pop-Location
        }
    }

    foreach ($db in $Databases) {
        $dbEscaped = $db.Replace("'", "''")
        $exists = docker exec $containerName psql -U $User -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$dbEscaped';"
        if ($LASTEXITCODE -ne 0) {
            throw "No se pudo consultar la base '$db' usando docker."
        }

        if (($exists | Out-String).Trim() -ne "1") {
            $quotedDb = '"' + $db.Replace('"', '""') + '"'
            docker exec $containerName psql -U $User -d postgres -c "CREATE DATABASE $quotedDb;" | Out-Host
            if ($LASTEXITCODE -ne 0) {
                throw "No se pudo crear la base '$db' usando docker."
            }
            Write-Info "Base creada: $db"
        }
        else {
            Write-Info "Base verificada: $db"
        }
    }
}

function Get-DotnetWithSdkMajor([int]$Major) {
    $candidates = [System.Collections.Generic.List[string]]::new()

    $globalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($globalDotnet) {
        $candidates.Add($globalDotnet.Source)
    }

    if ($env:USERPROFILE) {
        $localDotnet10 = Join-Path $env:USERPROFILE ".dotnet10\dotnet.exe"
        if (Test-Path $localDotnet10) {
            $candidates.Add($localDotnet10)
        }
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        $list = & $candidate --list-sdks 2>$null
        foreach ($line in $list) {
            if ($line -match "^$Major\.") {
                return $candidate
            }
        }
    }

    return $null
}

function Start-DockerService([string]$ServiceName) {
    Push-Location $Root
    try {
        docker compose up -d $ServiceName | Out-Host
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path $PidDir)) {
    New-Item -Path $PidDir -ItemType Directory | Out-Null
}

Write-Info "Cargando variables de entorno..."
Load-DotEnv -FilePath $EnvFile
Ensure-RequiredEnv
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable("Serilog__WriteTo__2__Args__serverUrl", "Process"))) {
    [Environment]::SetEnvironmentVariable("Serilog__WriteTo__2__Args__serverUrl", "http://localhost:5341", "Process")
}

$defaultConn = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Process")
$odontologiaConn = [Environment]::GetEnvironmentVariable("ConnectionStrings__OdontologiaConnection", "Process")
$academicoConn = [Environment]::GetEnvironmentVariable("ConnectionStrings__AcademicoConnection", "Process")

$defaultConnMap = Parse-ConnectionString $defaultConn
$dbHost = if ($defaultConnMap.ContainsKey("host")) { $defaultConnMap["host"] } else { "localhost" }
$dbPort = if ($defaultConnMap.ContainsKey("port")) { [int]$defaultConnMap["port"] } else { 5432 }
$dbUser = if ($defaultConnMap.ContainsKey("username")) { $defaultConnMap["username"] } else { "postgres" }
$dbPassword = if ($defaultConnMap.ContainsKey("password")) { $defaultConnMap["password"] } else { "" }

$databaseNames = @(
    (Parse-ConnectionString $defaultConn)["database"],
    (Parse-ConnectionString $odontologiaConn)["database"],
    (Parse-ConnectionString $academicoConn)["database"]
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

if ($databaseNames.Count -eq 0) {
    throw "No se detectaron nombres de bases en las cadenas de conexion."
}

$startedDockerPostgres = $false

Write-Info "Validando PostgreSQL en ${dbHost}:$dbPort ..."
if (-not (Test-TcpPort -HostName $dbHost -Port $dbPort)) {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if ($dockerCommand) {
        Write-Warn "No hay respuesta en ${dbHost}:$dbPort. Intentando levantar postgres por docker compose..."
        Push-Location $Root
        try {
            docker compose up -d postgres | Out-Host
            $startedDockerPostgres = $true
        }
        finally {
            Pop-Location
        }

        Start-Sleep -Seconds 3
    }

    if (-not (Test-TcpPort -HostName $dbHost -Port $dbPort)) {
        if (($dbHost -eq "localhost" -or $dbHost -eq "127.0.0.1") -and $dbPort -ne 5433 -and (Test-TcpPort -HostName "localhost" -Port 5433)) {
            Write-Warn "Se detecto PostgreSQL disponible en localhost:5433. Ajustando conexiones del proceso actual."
            $dbPort = 5433
            $defaultConn = Set-ConnectionPort -ConnectionString $defaultConn -Port $dbPort
            $odontologiaConn = Set-ConnectionPort -ConnectionString $odontologiaConn -Port $dbPort
            $academicoConn = Set-ConnectionPort -ConnectionString $academicoConn -Port $dbPort
            [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $defaultConn, "Process")
            [Environment]::SetEnvironmentVariable("ConnectionStrings__OdontologiaConnection", $odontologiaConn, "Process")
            [Environment]::SetEnvironmentVariable("ConnectionStrings__AcademicoConnection", $academicoConn, "Process")
        }
        else {
            throw "No fue posible conectar a PostgreSQL en ${dbHost}:$dbPort."
        }
    }
}

Write-Info "Verificando/creando bases de datos requeridas..."
$createdWithPsql = $false
if (-not [string]::IsNullOrWhiteSpace($dbPassword)) {
    $createdWithPsql = Ensure-DatabasesWithPsql -HostName $dbHost -Port $dbPort -User $dbUser -Password $dbPassword -Databases $databaseNames
}

if (-not $createdWithPsql) {
    Ensure-DatabasesWithDocker -User $dbUser -Databases $databaseNames
}

Stop-TrackedProcess -PidFileName "backend.pid"
Stop-TrackedProcess -PidFileName "frontend.pid"
Stop-TrackedProcess -PidFileName "browser.pid"

Write-Info "Iniciando backend..."
$backendStarted = $false
$backendViaDocker = $false
$apiHealthUrl = "http://localhost:5154/health"
$dotnetWithSdk10 = Get-DotnetWithSdkMajor -Major 10
if ($dotnetWithSdk10) {
    [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Process")
    $backendProject = Join-Path $Root "MEDICSYS.Api\MEDICSYS.Api.csproj"
    $backendProcess = Start-Process -FilePath $dotnetWithSdk10 -ArgumentList @("run", "--project", $backendProject, "--urls", "http://localhost:5154") -WorkingDirectory $Root -PassThru
    Start-Sleep -Seconds 2
    if (-not $backendProcess.HasExited) {
        if (Wait-HttpOk -Url $apiHealthUrl) {
            $backendProcess.Id | Set-Content -Path (Join-Path $PidDir "backend.pid")
            Write-Ok "Backend iniciado localmente con $dotnetWithSdk10 (PID $($backendProcess.Id))"
            $backendStarted = $true
        }
        else {
            Stop-Process -Id $backendProcess.Id -Force -ErrorAction SilentlyContinue
            Write-Warn "El backend local no respondio healthcheck. Se usara docker compose para API."
        }
    }
    else {
        Write-Warn "El backend local finalizo al iniciar. Se usara docker compose para API."
    }
}
else {
    Write-Warn "No se detecto SDK .NET 10 en Windows. Se usara docker compose para API."
}

if (-not $backendStarted) {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCommand) {
        throw "No fue posible iniciar backend local ni por docker (docker no disponible)."
    }

    Start-DockerService -ServiceName "api"
    $backendViaDocker = $true
    if (-not (Wait-HttpOk -Url $apiHealthUrl)) {
        throw "El backend en docker compose no respondio healthcheck en $apiHealthUrl."
    }

    Remove-Item (Join-Path $PidDir "backend.pid") -Force -ErrorAction SilentlyContinue
    Write-Ok "Backend iniciado en docker compose (servicio api)."
}

Write-Info "Iniciando frontend..."
$npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
$npmPath = if ($npmCommand) { $npmCommand.Source } else { Join-Path $env:ProgramFiles "nodejs\npm.cmd" }
if (-not (Test-Path $npmPath)) {
    throw "No se encontro npm.cmd. Instala Node.js para continuar."
}

$frontendProcess = Start-Process -FilePath $npmPath -ArgumentList @("start") -WorkingDirectory (Join-Path $Root "MEDICSYS.Web") -PassThru
$frontendProcess.Id | Set-Content -Path (Join-Path $PidDir "frontend.pid")
Write-Ok "Frontend iniciado (PID $($frontendProcess.Id))"

try {
    $browserProcess = Start-Process "http://localhost:4200" -PassThru
    $browserProcess.Id | Set-Content -Path (Join-Path $PidDir "browser.pid")
}
catch {
    Write-Warn "No se pudo registrar el proceso del navegador. El sistema igual quedo iniciado."
}

if ($startedDockerPostgres) {
    Set-Content -Path $DockerMarker -Value "started-by-start-script"
}
else {
    Remove-Item $DockerMarker -Force -ErrorAction SilentlyContinue
}

if ($backendViaDocker) {
    Set-Content -Path $ApiDockerMarker -Value "started-by-start-script"
}
else {
    Remove-Item $ApiDockerMarker -Force -ErrorAction SilentlyContinue
}

Write-Ok "Inicio completado. API: http://localhost:5154 | Web: http://localhost:4200"
