param(
    [string]$BaseUrl = "http://localhost:5154",
    [string]$Email = "odontologo@medicsys.com",
    [string]$Password = "Odontologo123!",
    [int]$RequestsPerScenario = 35,
    [int]$Concurrency = 8,
    [int]$WarmupRequests = 3
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if (-not $Values -or $Values.Count -eq 0) {
        return 0
    }

    $sorted = @($Values | Sort-Object)
    $rank = [Math]::Ceiling(($Percentile / 100.0) * $sorted.Count) - 1
    if ($rank -lt 0) { $rank = 0 }
    if ($rank -ge $sorted.Count) { $rank = $sorted.Count - 1 }
    return [Math]::Round([double]$sorted[$rank], 2)
}

function Invoke-Warmup {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [string]$BodyJson,
        [int]$Count
    )

    for ($i = 0; $i -lt $Count; $i++) {
        try {
            if ($Method -eq "GET") {
                Invoke-WebRequest -Uri $Uri -Method Get -Headers $Headers -TimeoutSec 45 -UseBasicParsing | Out-Null
            }
            else {
                Invoke-WebRequest -Uri $Uri -Method $Method -Headers $Headers -ContentType "application/json" -Body $BodyJson -TimeoutSec 45 -UseBasicParsing | Out-Null
            }
        }
        catch {
            # Warmup best effort
        }
    }
}

function Invoke-Scenario {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [string]$BodyJson,
        [int]$Requests,
        [int]$Throttle,
        [int]$Warmup
    )

    Write-Host "-> Escenario: $Name ($Method $Uri)" -ForegroundColor Cyan
    Invoke-Warmup -Method $Method -Uri $Uri -Headers $Headers -BodyJson $BodyJson -Count $Warmup

    $startedAt = Get-Date
    $results = New-Object System.Collections.Generic.List[object]

    for ($offset = 0; $offset -lt $Requests; $offset += $Throttle) {
        $batchSize = [Math]::Min($Throttle, $Requests - $offset)
        $jobs = @()

        for ($i = 0; $i -lt $batchSize; $i++) {
            $jobs += Start-Job -ScriptBlock {
                param($Method, $Uri, $Headers, $BodyJson)

                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                try {
                    if ($Method -eq "GET") {
                        $resp = Invoke-WebRequest -Uri $Uri -Method Get -Headers $Headers -TimeoutSec 60 -UseBasicParsing
                    }
                    else {
                        $resp = Invoke-WebRequest -Uri $Uri -Method $Method -Headers $Headers -ContentType "application/json" -Body $BodyJson -TimeoutSec 60 -UseBasicParsing
                    }

                    $sw.Stop()
                    [pscustomobject]@{
                        Ok     = ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300)
                        Status = [int]$resp.StatusCode
                        Ms     = [double]$sw.Elapsed.TotalMilliseconds
                        Error  = ""
                    }
                }
                catch {
                    $sw.Stop()
                    $status = 0
                    try {
                        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                            $status = [int]$_.Exception.Response.StatusCode
                        }
                    }
                    catch {
                        $status = 0
                    }

                    [pscustomobject]@{
                        Ok     = $false
                        Status = $status
                        Ms     = [double]$sw.Elapsed.TotalMilliseconds
                        Error  = $_.Exception.Message
                    }
                }
            } -ArgumentList $Method, $Uri, $Headers, $BodyJson
        }

        if ($jobs.Count -gt 0) {
            $null = Wait-Job -Job $jobs
            $batchResults = Receive-Job -Job $jobs
            foreach ($item in $batchResults) {
                $results.Add($item)
            }
            Remove-Job -Job $jobs -Force
        }
    }

    $finishedAt = Get-Date
    $elapsed = ($finishedAt - $startedAt).TotalSeconds
    if ($elapsed -le 0) { $elapsed = 0.001 }

    $latencies = @($results | ForEach-Object { [double]$_.Ms })
    $successCount = @($results | Where-Object { $_.Ok }).Count
    $errorCount = $Requests - $successCount
    $errorRate = if ($Requests -gt 0) { [Math]::Round(($errorCount / $Requests) * 100, 2) } else { 0 }
    $avgMs = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Average).Average, 2) } else { 0 }
    $minMs = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Minimum).Minimum, 2) } else { 0 }
    $maxMs = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Maximum).Maximum, 2) } else { 0 }
    $p50 = Get-Percentile -Values $latencies -Percentile 50
    $p95 = Get-Percentile -Values $latencies -Percentile 95
    $p99 = Get-Percentile -Values $latencies -Percentile 99
    $rps = [Math]::Round(($Requests / $elapsed), 2)
    $statusBreakdown = (@($results | Group-Object Status | Sort-Object Name | ForEach-Object { "$($_.Name):$($_.Count)" })) -join ", "

    return [pscustomobject]@{
        Scenario   = $Name
        Method     = $Method
        Requests   = $Requests
        Concurrency= $Throttle
        Success    = $successCount
        Errors     = $errorCount
        ErrorRate  = $errorRate
        RPS        = $rps
        AvgMs      = $avgMs
        P50Ms      = $p50
        P95Ms      = $p95
        P99Ms      = $p99
        MinMs      = $minMs
        MaxMs      = $maxMs
        Statuses   = $statusBreakdown
    }
}

Write-Host "===============================================" -ForegroundColor Yellow
Write-Host " MEDICSYS - PRUEBAS DE RENDIMIENTO API" -ForegroundColor Yellow
Write-Host "===============================================" -ForegroundColor Yellow
Write-Host "BaseUrl: $BaseUrl"
Write-Host "Requests por escenario: $RequestsPerScenario"
Write-Host "Concurrencia: $Concurrency"
Write-Host ""

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 8
    if ($health.StatusCode -ne 200) {
        throw "Health check no exitoso: $($health.StatusCode)"
    }
}
catch {
    throw "La API no está disponible en $BaseUrl"
}

$loginPayload = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

$authResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginPayload
if (-not $authResponse.token) {
    throw "No se obtuvo token de autenticación."
}

$authHeaders = @{ Authorization = "Bearer $($authResponse.token)" }
$today = Get-Date
$from = $today.AddMonths(-5).ToString("yyyy-MM-dd")
$to = $today.AddDays(1).ToString("yyyy-MM-dd")

$scenarios = @(
    @{
        Name = "Login"
        Method = "POST"
        Uri = "$BaseUrl/api/auth/login"
        Headers = @{}
        BodyJson = $loginPayload
    },
    @{
        Name = "Historias clinicas (GET)"
        Method = "GET"
        Uri = "$BaseUrl/api/clinical-histories"
        Headers = $authHeaders
        BodyJson = ""
    },
    @{
        Name = "Gastos resumen"
        Method = "GET"
        Uri = "$BaseUrl/api/odontologia/gastos/summary"
        Headers = $authHeaders
        BodyJson = ""
    },
    @{
        Name = "Gastos listado 5 meses"
        Method = "GET"
        Uri = "$BaseUrl/api/odontologia/gastos?startDate=$from&endDate=$to"
        Headers = $authHeaders
        BodyJson = ""
    },
    @{
        Name = "Compras listado 5 meses"
        Method = "GET"
        Uri = "$BaseUrl/api/odontologia/compras?dateFrom=$from&dateTo=$to"
        Headers = $authHeaders
        BodyJson = ""
    },
    @{
        Name = "Contabilidad resumen 5 meses"
        Method = "GET"
        Uri = "$BaseUrl/api/accounting/summary?from=$from&to=$to"
        Headers = $authHeaders
        BodyJson = ""
    },
    @{
        Name = "Reporte financiero 5 meses"
        Method = "GET"
        Uri = "$BaseUrl/api/odontologia/reportes/financiero?startDate=$from&endDate=$to"
        Headers = $authHeaders
        BodyJson = ""
    }
)

$report = New-Object System.Collections.Generic.List[object]
foreach ($scenario in $scenarios) {
    $result = Invoke-Scenario `
        -Name $scenario.Name `
        -Method $scenario.Method `
        -Uri $scenario.Uri `
        -Headers $scenario.Headers `
        -BodyJson $scenario.BodyJson `
        -Requests $RequestsPerScenario `
        -Throttle $Concurrency `
        -Warmup $WarmupRequests

    $report.Add($result)
    Start-Sleep -Milliseconds 750
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $PSScriptRoot "artifacts\perf"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$jsonPath = Join-Path $outDir "api-performance-$timestamp.json"
$csvPath = Join-Path $outDir "api-performance-$timestamp.csv"

$report | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8
$report | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

Write-Host ""
Write-Host "================ RESULTADOS ================" -ForegroundColor Green
$report | Format-Table Scenario, Requests, Concurrency, Success, Errors, ErrorRate, RPS, AvgMs, P95Ms, P99Ms, Statuses -AutoSize
Write-Host "===========================================" -ForegroundColor Green
Write-Host "JSON: $jsonPath"
Write-Host "CSV : $csvPath"
