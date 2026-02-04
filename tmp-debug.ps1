$loginBody = @{ email = 'odontologo@medicsys.com'; password = 'Odontologo123!' } | ConvertTo-Json
$loginResponse = Invoke-WebRequest -Uri 'http://localhost:5154/api/auth/login' -Method POST -ContentType 'application/json' -Body $loginBody
$token = ($loginResponse.Content | ConvertFrom-Json).token
$resp = Invoke-WebRequest -Uri 'http://localhost:5154/api/clinical-histories' -Headers @{ Authorization = "Bearer $token" } -SkipHttpErrorCheck
Write-Output ('STATUS: ' + $resp.StatusCode)
Write-Output $resp.Content
