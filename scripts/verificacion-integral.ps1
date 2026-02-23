
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$projectRoot = Split-Path -Parent $PSScriptRoot
$apiUrl = 'http://localhost:5105'
$dotnet = 'C:\Users\gerso\.dotnet10\dotnet.exe'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$dbHost = 'localhost'
$dbPort = '5433'
$dbUser = 'postgres'
$dbPassword = '030762'

$env:DOTNET_ROOT = 'C:\Users\gerso\.dotnet10'
$env:PATH = "C:\Users\gerso\.dotnet10;$env:PATH"
$env:PGPASSWORD = $dbPassword

$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$results = [System.Collections.Generic.List[object]]::new()

function Add-Result {
    param([string]$Layer,[string]$Test,[bool]$Pass,[string]$Details)
    $results.Add([PSCustomObject]@{ Layer=$Layer; Test=$Test; Pass=$Pass; Details=$Details })
}

function Expect {
    param([bool]$Condition,[string]$Layer,[string]$Test,[string]$PassDetails,[string]$FailDetails)
    if ($Condition) { Add-Result $Layer $Test $true $PassDetails } else { Add-Result $Layer $Test $false $FailDetails }
}

function Invoke-Api {
    param([string]$Method,[string]$Path,[string]$Token,$Body)
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    $uri = "$apiUrl$Path"

    try {
        if ($null -ne $Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 15
            $res = Invoke-WebRequest -Uri $uri -Method $Method -Headers $headers -ContentType 'application/json' -Body $jsonBody -UseBasicParsing
        } else {
            $res = Invoke-WebRequest -Uri $uri -Method $Method -Headers $headers -UseBasicParsing
        }

        $obj = $null
        if ($res.Content) { try { $obj = $res.Content | ConvertFrom-Json } catch {} }
        return [PSCustomObject]@{ StatusCode=[int]$res.StatusCode; Body=$obj; Raw=$res.Content }
    }
    catch {
        $status = 0
        $raw = $_.ErrorDetails.Message
        if ($_.Exception.Response) {
            try { $status = [int]$_.Exception.Response.StatusCode } catch {}
        }
        $obj = $null
        if ($raw) { try { $obj = $raw | ConvertFrom-Json } catch {} }
        return [PSCustomObject]@{ StatusCode=$status; Body=$obj; Raw=$raw }
    }
}

function Sql-Scalar {
    param([string]$Database,[string]$Sql)
    $v = $Sql | & $psql -h $dbHost -p $dbPort -U $dbUser -d $Database -At
    if ($LASTEXITCODE -ne 0) { throw "SQL error: $Sql" }
    if ($null -eq $v) { return '' }
    return ($v | Select-Object -First 1).ToString().Trim()
}

function Sql-Count {
    param([string]$Database,[string]$Sql)
    $v = Sql-Scalar $Database $Sql
    if (-not $v) { return 0 }
    return [int]$v
}

function To-Dec {
    param($Value)
    if ($null -eq $Value) { return [decimal]0 }
    $t = $Value.ToString().Replace(',', '.')
    return [decimal]::Parse($t, [System.Globalization.CultureInfo]::InvariantCulture)
}

$apiJob = Start-Job -ScriptBlock {
    param($projectRoot, $dotnet, $apiUrl)
    Set-Location $projectRoot
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Serilog__WriteTo__2__Args__serverUrl = 'http://localhost:5341'
    & $dotnet run --project MEDICSYS.Api/MEDICSYS.Api.csproj --urls $apiUrl
} -ArgumentList $projectRoot, $dotnet, $apiUrl

try {
    $dbNames = & $psql -h $dbHost -p $dbPort -U $dbUser -d postgres -At -c "SELECT datname FROM pg_database WHERE datname IN ('medicsys','medicsys_academico','medicsys_odontologia') ORDER BY datname;"
    $dbList = @($dbNames)
    $allDbs = @('medicsys','medicsys_academico','medicsys_odontologia') | ForEach-Object { $dbList -contains $_ }
    Expect (($allDbs | Where-Object { $_ }).Count -eq 3) 'Infra' 'Bases locales' ($dbList -join ', ') "Faltan BD: $($dbList -join ', ')"

    $ready = $false
    for ($i=0; $i -lt 45; $i++) {
        Start-Sleep -Seconds 2
        if ((Invoke-Api 'GET' '/health' '' $null).StatusCode -eq 200) { $ready = $true; break }
    }
    Expect $ready 'Infra' 'Health API' 'API OK' 'API no respondió'
    if (-not $ready) { throw 'API no disponible' }

    $appUpdate = (& $dotnet dotnet-ef database update --context AppDbContext --project MEDICSYS.Api/MEDICSYS.Api.csproj --startup-project MEDICSYS.Api/MEDICSYS.Api.csproj 2>&1 | Out-String)
    $odUpdate = (& $dotnet dotnet-ef database update --context OdontologoDbContext --project MEDICSYS.Api/MEDICSYS.Api.csproj --startup-project MEDICSYS.Api/MEDICSYS.Api.csproj 2>&1 | Out-String)
    $acUpdate = (& $dotnet dotnet-ef database update --context AcademicDbContext --project MEDICSYS.Api/MEDICSYS.Api.csproj --startup-project MEDICSYS.Api/MEDICSYS.Api.csproj 2>&1 | Out-String)

    Expect (-not ($appUpdate -match 'PendingModelChangesWarning')) 'Infra' 'Migraciones AppDbContext' 'Sin pendientes' 'PendingModelChangesWarning detectado'
    Expect ($odUpdate -match 'already up to date') 'Infra' 'Migraciones Odontologia' 'Al día' 'No está al día'
    Expect ($acUpdate -match 'already up to date') 'Infra' 'Migraciones Academico' 'Al día' 'No está al día'

    $adminLogin = Invoke-Api 'POST' '/api/auth/login' '' @{ email='admin@medicsys.com'; password='Admin123!' }
    $profLogin = Invoke-Api 'POST' '/api/auth/login' '' @{ email='profesor@medicsys.com'; password='Profesor123!' }
    $studentLogin = Invoke-Api 'POST' '/api/auth/login' '' @{ email='estudiante1@medicsys.com'; password='Estudiante123!' }
    $odLogin = Invoke-Api 'POST' '/api/auth/login' '' @{ email='odontologo@medicsys.com'; password='Odontologo123!' }

    $adminToken = $adminLogin.Body.token
    $profToken = $profLogin.Body.token
    $studentToken = $studentLogin.Body.token
    $odToken = $odLogin.Body.token

    Expect ($adminLogin.StatusCode -eq 200 -and $adminToken) 'Auth' 'Login admin' 'OK' "Status=$($adminLogin.StatusCode)"
    Expect ($profLogin.StatusCode -eq 200 -and $profToken) 'Auth' 'Login profesor' 'OK' "Status=$($profLogin.StatusCode)"
    Expect ($studentLogin.StatusCode -eq 200 -and $studentToken) 'Auth' 'Login alumno' 'OK' "Status=$($studentLogin.StatusCode)"
    Expect ($odLogin.StatusCode -eq 200 -and $odToken) 'Auth' 'Login odontologo' 'OK' "Status=$($odLogin.StatusCode)"

    $profId = (Invoke-Api 'GET' '/api/auth/me' $profToken $null).Body.id
    $studentId = (Invoke-Api 'GET' '/api/auth/me' $studentToken $null).Body.id
    $odId = (Invoke-Api 'GET' '/api/auth/me' $odToken $null).Body.id
    $newStudentEmail = "audit.student.$stamp@medicsys.com"
    $regStudent = Invoke-Api 'POST' '/api/auth/register-student' '' @{ email=$newStudentEmail; password='Estudiante123!'; fullName="Audit Student $stamp"; universityId="AUD-$stamp" }
    $regStudentDb = Sql-Count 'medicsys_academico' "SELECT COUNT(*) FROM ""AspNetUsers"" WHERE ""Email"" = '$newStudentEmail';"
    Expect ($regStudent.StatusCode -eq 200 -and $regStudentDb -eq 1) 'Auth' 'Registro alumno' $newStudentEmail "Status=$($regStudent.StatusCode), db=$regStudentDb"

    $newProfEmail = "audit.prof.$stamp@medicsys.com"
    $regProfAdmin = Invoke-Api 'POST' '/api/auth/register-professor' $adminToken @{ email=$newProfEmail; password='Profesor123!'; fullName="Audit Professor $stamp"; universityId="PROF-$stamp" }
    $regProfDb = Sql-Count 'medicsys_academico' "SELECT COUNT(*) FROM ""AspNetUsers"" WHERE ""Email"" = '$newProfEmail';"
    Expect ($regProfAdmin.StatusCode -eq 200 -and $regProfDb -eq 1) 'Auth' 'Admin registra profesor' $newProfEmail "Status=$($regProfAdmin.StatusCode), db=$regProfDb"

    $regProfForbidden = Invoke-Api 'POST' '/api/auth/register-professor' $profToken @{ email="forbidden.$stamp@medicsys.com"; password='Profesor123!'; fullName='X'; universityId='X' }
    Expect ($regProfForbidden.StatusCode -eq 403) 'Auth' 'Profesor bloqueado al registrar profesor' '403 esperado' "Status=$($regProfForbidden.StatusCode)"

    $usersAll = Invoke-Api 'GET' '/api/users/admin/all' $adminToken $null
    $usersStudents = Invoke-Api 'GET' '/api/users/students' $adminToken $null
    Expect ($usersAll.StatusCode -eq 200 -and $usersStudents.StatusCode -eq 200) 'Usuarios' 'Listados usuarios' 'OK' "admin/all=$($usersAll.StatusCode), students=$($usersStudents.StatusCode)"

    $crudEmail = "audit.user.$stamp@medicsys.com"
    $createUser = Invoke-Api 'POST' '/api/users/admin' $adminToken @{ email=$crudEmail; password='Estudiante123!'; fullName="User $stamp"; universityId="U-$stamp"; role='Alumno' }
    $crudId = $createUser.Body.id
    $updateUser = Invoke-Api 'PUT' "/api/users/admin/$crudId" $adminToken @{ fullName="User Updated $stamp"; universityId="UU-$stamp" }
    $deleteUser = Invoke-Api 'DELETE' "/api/users/admin/$crudId" $adminToken $null
    $crudDb = Sql-Count 'medicsys_academico' "SELECT COUNT(*) FROM ""AspNetUsers"" WHERE ""Email"" = '$crudEmail';"
    Expect ($createUser.StatusCode -eq 200 -and $updateUser.StatusCode -eq 200 -and $deleteUser.StatusCode -eq 204 -and $crudDb -eq 0) 'Usuarios' 'CRUD admin usuarios' 'OK' "create=$($createUser.StatusCode), update=$($updateUser.StatusCode), delete=$($deleteUser.StatusCode), db=$crudDb"

    $patientIdNumber = "09$stamp"
    $createPatient = Invoke-Api 'POST' '/api/patients' $odToken @{ firstName='Paciente'; lastName="App$stamp"; idNumber=$patientIdNumber; dateOfBirth='1991-03-15T00:00:00Z'; gender='F'; address='Cuenca'; phone='0990000001'; email="app.patient.$stamp@mail.com"; emergencyContact='C'; emergencyPhone='0990000002'; allergies='Ninguna'; medications=''; diseases=''; bloodType='O+'; notes='Audit' }
    $appPatientId = $createPatient.Body.id
    $updatePatient = Invoke-Api 'PUT' "/api/patients/$appPatientId" $odToken @{ phone='0999999999'; address='Cuenca Centro' }
    $patientDb = Sql-Count 'medicsys' "SELECT COUNT(*) FROM ""Patients"" WHERE ""Id"" = '$appPatientId';"
    Expect ($createPatient.StatusCode -eq 201 -and $updatePatient.StatusCode -eq 200 -and $patientDb -eq 1) 'AppDB' 'Pacientes app' "PatientId=$appPatientId" "create=$($createPatient.StatusCode), update=$($updatePatient.StatusCode), db=$patientDb"

    $start = (Get-Date).AddHours(6).ToUniversalTime().ToString('o')
    $end = (Get-Date).AddHours(7).ToUniversalTime().ToString('o')
    $createAgenda = Invoke-Api 'POST' '/api/agenda/appointments' $odToken @{ patientName="Paciente Agenda $stamp"; reason='Control'; startAt=$start; endAt=$end; notes='Agenda'; status='Pending' }
    $appAppointmentId = $createAgenda.Body.id
    $reviewAgenda = Invoke-Api 'POST' "/api/agenda/appointments/$appAppointmentId/review" $odToken @{ approved=$true; notes='OK' }
    $agendaDb = Sql-Count 'medicsys' "SELECT COUNT(*) FROM ""Appointments"" WHERE ""Id"" = '$appAppointmentId';"
    $remindersDb = Sql-Count 'medicsys' "SELECT COUNT(*) FROM ""Reminders"" WHERE ""AppointmentId"" = '$appAppointmentId';"
    Expect ($createAgenda.StatusCode -eq 200 -and $reviewAgenda.StatusCode -eq 200 -and $agendaDb -eq 1 -and $remindersDb -ge 2) 'AppDB' 'Agenda y recordatorios' "Appointment=$appAppointmentId reminders=$remindersDb" "create=$($createAgenda.StatusCode), review=$($reviewAgenda.StatusCode), db=$agendaDb/$remindersDb"

    $createHistory = Invoke-Api 'POST' '/api/clinical-histories' $studentToken @{ data=@{ personal=@{ firstName='Paciente'; lastName="Historia$stamp"; idNumber="08$stamp" }; motivo='Dolor' } }
    $historyId = $createHistory.Body.id
    $submitHistory = Invoke-Api 'POST' "/api/clinical-histories/$historyId/submit" $studentToken @{}
    $reviewHistory = Invoke-Api 'POST' "/api/clinical-histories/$historyId/review" $profToken @{ approved=$true; notes='Aprobado' }
    $historyStatus = Sql-Scalar 'medicsys' "SELECT ""Status""::text FROM ""ClinicalHistories"" WHERE ""Id"" = '$historyId';"
    Expect ($createHistory.StatusCode -eq 201 -and $submitHistory.StatusCode -eq 200 -and $reviewHistory.StatusCode -eq 200 -and ($historyStatus -eq 'Approved' -or $historyStatus -eq '2')) 'AppDB' 'Historia clínica app' "History=$historyId status=$historyStatus" "create=$($createHistory.StatusCode), submit=$($submitHistory.StatusCode), review=$($reviewHistory.StatusCode), status=$historyStatus"

    $createAcademicPatient = Invoke-Api 'POST' '/api/academic/patients' $profToken @{ firstName='Paciente'; lastName="Academico$stamp"; idNumber="07$stamp"; dateOfBirth='1990-01-01T00:00:00Z'; gender='M'; phone='0991'; email="academic.$stamp@mail.com"; address='Cuenca'; bloodType='A+'; allergies=''; medicalConditions=''; emergencyContact='F'; emergencyPhone='0992' }
    $academicPatientId = $createAcademicPatient.Body.id
    $academicPatientDb = Sql-Count 'medicsys_academico' "SELECT COUNT(*) FROM ""AcademicPatients"" WHERE ""Id"" = '$academicPatientId';"
    Expect ($createAcademicPatient.StatusCode -eq 201 -and $academicPatientDb -eq 1) 'Academico' 'Paciente académico' "AcademicPatient=$academicPatientId" "create=$($createAcademicPatient.StatusCode), db=$academicPatientDb"

    $acadStart = (Get-Date).AddHours(8).ToUniversalTime().ToString('o')
    $acadEnd = (Get-Date).AddHours(9).ToUniversalTime().ToString('o')
    $createAcademicAppointment = Invoke-Api 'POST' '/api/academic/appointments' $profToken @{ studentId=$studentId; patientName="Paciente Cita $stamp"; reason='Practica'; startAt=$acadStart; endAt=$acadEnd; notes='Acad'; status='Pending' }
    $acadApptId = $createAcademicAppointment.Body.id
    $reviewAcademicAppointment = Invoke-Api 'POST' "/api/academic/appointments/$acadApptId/review" $profToken @{ approved=$true; notes='OK' }
    $acadRemindersDb = Sql-Count 'medicsys_academico' "SELECT COUNT(*) FROM ""AcademicReminders"" WHERE ""AppointmentId"" = '$acadApptId';"
    Expect ($createAcademicAppointment.StatusCode -eq 200 -and $reviewAcademicAppointment.StatusCode -eq 200 -and $acadRemindersDb -ge 2) 'Academico' 'Cita académica y recordatorios' "Appointment=$acadApptId reminders=$acadRemindersDb" "create=$($createAcademicAppointment.StatusCode), review=$($reviewAcademicAppointment.StatusCode), reminders=$acadRemindersDb"
    $createAcademicHistory = Invoke-Api 'POST' '/api/academic/clinical-histories' $studentToken @{ data=@{ personal=@{ firstName='Paciente'; lastName="AcadHist$stamp"; idNumber="06$stamp" }; motivo='Control' } }
    $academicHistoryId = $createAcademicHistory.Body.id
    $submitAcademicHistory = Invoke-Api 'POST' "/api/academic/clinical-histories/$academicHistoryId/submit" $studentToken @{}
    $template = Invoke-Api 'POST' '/api/academic/clinical-histories/comment-templates' $profToken @{ title="Template $stamp"; commentText='Revisar'; category='General' }
    $templateId = $template.Body.id
    $batch = Invoke-Api 'POST' '/api/academic/clinical-histories/batch-review' $profToken @{ historyIds=@($academicHistoryId); decision='approve'; reviewNotes='Aprobada'; grade=9; templateIds=@($templateId) }
    $dashboard = Invoke-Api 'GET' '/api/academic/clinical-histories/dashboard' $profToken $null
    $templateUsage = Sql-Scalar 'medicsys_academico' "SELECT ""UsageCount"" FROM ""AcademicReviewCommentTemplates"" WHERE ""Id"" = '$templateId';"
    Expect ($createAcademicHistory.StatusCode -eq 201 -and $submitAcademicHistory.StatusCode -eq 200 -and $template.StatusCode -eq 201 -and $batch.StatusCode -eq 200 -and $dashboard.StatusCode -eq 200 -and [int]$templateUsage -ge 1) 'Academico' 'Historia académica + batch + dashboard' "History=$academicHistoryId templateUsage=$templateUsage" "create=$($createAcademicHistory.StatusCode), submit=$($submitAcademicHistory.StatusCode), template=$($template.StatusCode), batch=$($batch.StatusCode), dashboard=$($dashboard.StatusCode), usage=$templateUsage"

    $createOdPatient = Invoke-Api 'POST' '/api/odontologia/patients' $odToken @{ firstName='Paciente'; lastName="Odonto$stamp"; idNumber="05$stamp"; dateOfBirth='1993-09-09'; gender='F'; address='Cuenca'; phone='0998880001'; email="od.patient.$stamp@mail.com" }
    $odPatientId = $createOdPatient.Body.id
    $createOdAppointment = Invoke-Api 'POST' '/api/odontologia/appointments' $odToken @{ patientName="Paciente Odonto $stamp"; reason='Limpieza'; startAt=(Get-Date).AddHours(3).ToUniversalTime().ToString('o'); endAt=(Get-Date).AddHours(4).ToUniversalTime().ToString('o'); notes='Cita'; status='Pending' }
    $odAppointmentId = $createOdAppointment.Body.id
    $odPatientDb = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""OdontologoPatients"" WHERE ""Id"" = '$odPatientId';"
    $odApptDb = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""OdontologoAppointments"" WHERE ""Id"" = '$odAppointmentId';"
    Expect ($createOdPatient.StatusCode -eq 201 -and $createOdAppointment.StatusCode -eq 200 -and $odPatientDb -eq 1 -and $odApptDb -eq 1) 'Odontologia' 'Pacientes y citas odonto' 'OK' "patient=$($createOdPatient.StatusCode), appointment=$($createOdAppointment.StatusCode), db=$odPatientDb/$odApptDb"

    $createInventory = Invoke-Api 'POST' '/api/odontologia/inventory' $odToken @{ name="Item Audit $stamp"; description='Material'; sku="SKU-$stamp"; quantity=12; minimumQuantity=3; unitPrice=4.5; supplier='Proveedor' }
    $inventoryId = $createInventory.Body.id
    $kEntry = Invoke-Api 'POST' '/api/odontologia/kardex/movements/entry' $odToken @{ inventoryItemId=$inventoryId; quantity=5; unitPrice=4.8; reference='Ingreso' }
    $kExit = Invoke-Api 'POST' '/api/odontologia/kardex/movements/exit' $odToken @{ inventoryItemId=$inventoryId; quantity=3; unitPrice=4.8; reference='Salida' }
    $kAdjust = Invoke-Api 'POST' '/api/odontologia/kardex/movements/adjustment' $odToken @{ inventoryItemId=$inventoryId; newQuantity=20; reason='Ajuste' }
    $inventoryQty = Sql-Scalar 'medicsys_odontologia' "SELECT ""Quantity"" FROM ""InventoryItems"" WHERE ""Id"" = '$inventoryId';"
    $kMov = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""InventoryMovements"" WHERE ""InventoryItemId"" = '$inventoryId';"
    Expect ($createInventory.StatusCode -eq 201 -and $kEntry.StatusCode -eq 200 -and $kExit.StatusCode -eq 200 -and $kAdjust.StatusCode -eq 200 -and [int]$inventoryQty -eq 20 -and $kMov -ge 3) 'Odontologia' 'Inventario y kardex' "Item=$inventoryId qty=$inventoryQty mov=$kMov" "create=$($createInventory.StatusCode), entry=$($kEntry.StatusCode), exit=$($kExit.StatusCode), adjust=$($kAdjust.StatusCode), qty=$inventoryQty, mov=$kMov"

    $purchase = Invoke-Api 'POST' '/api/odontologia/compras' $odToken @{ supplier='Proveedor Compras'; invoiceNumber="FAC-$stamp"; purchaseDate=(Get-Date).ToUniversalTime().ToString('o'); notes='Compra'; status='Pending'; items=@(@{ inventoryItemId=$inventoryId; quantity=4; unitPrice=5.1 }) }
    $purchaseId = $purchase.Body.id
    $receive = Invoke-Api 'POST' "/api/odontologia/compras/$purchaseId/receive" $odToken @{}
    $expense = Invoke-Api 'POST' '/api/odontologia/gastos' $odToken @{ description="Gasto $stamp"; amount=18.75; expenseDate=(Get-Date).ToUniversalTime().ToString('o'); category='Marketing'; paymentMethod='Efectivo'; invoiceNumber="GASTO-$stamp"; supplier='Proveedor'; notes='Gasto' }
    $expenseId = $expense.Body.id
    $purchaseEntry = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""AccountingEntries"" WHERE ""Source"" = 'Purchase' AND ""Reference"" = '$purchaseId';"
    $expenseEntry = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""AccountingEntries"" WHERE ""Source"" = 'Expense' AND ""Reference"" = '$expenseId';"
    Expect ($purchase.StatusCode -eq 201 -and $receive.StatusCode -eq 200 -and $expense.StatusCode -eq 201 -and $purchaseEntry -ge 1 -and $expenseEntry -ge 1) 'Odontologia' 'Compras + gastos + asientos' "purchase=$purchaseId expense=$expenseId" "purchase=$($purchase.StatusCode), receive=$($receive.StatusCode), expense=$($expense.StatusCode), entries=$purchaseEntry/$expenseEntry"

    $invoice = Invoke-Api 'POST' '/api/invoices' $odToken @{ customerIdentificationType='05'; customerIdentification=$patientIdNumber; customerName="Paciente Factura $stamp"; customerAddress='Cuenca'; customerPhone='0992222222'; customerEmail="invoice.$stamp@mail.com"; observations='Factura'; paymentMethod='Cash'; sendToSri=$false; sriEnvironment='Pruebas'; items=@(@{ description="Servicio $stamp"; quantity=1; unitPrice=45; discountPercent=0 }) }
    $invoiceId = $invoice.Body.id
    $invoiceOwnership = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""OdontologoInvoiceOwnerships"" WHERE ""InvoiceId"" = '$invoiceId' AND ""OdontologoId"" = '$odId';"
    $invoiceAccOwnership = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""OdontologoAccountingEntryOwnerships"" o JOIN ""AccountingEntries"" a ON a.""Id"" = o.""AccountingEntryId"" WHERE a.""InvoiceId"" = '$invoiceId' AND o.""OdontologoId"" = '$odId';"
    Expect ($invoice.StatusCode -eq 201 -and $invoiceOwnership -eq 1 -and $invoiceAccOwnership -ge 1) 'Odontologia' 'Factura + ownership' "Invoice=$invoiceId" "invoice=$($invoice.StatusCode), own=$invoiceOwnership, accOwn=$invoiceAccOwnership"

    $categories = Invoke-Api 'GET' '/api/accounting/categories' $odToken $null
    $manual = Invoke-Api 'POST' '/api/accounting/entries' $odToken @{ date=(Get-Date).ToUniversalTime().ToString('o'); type='Expense'; categoryId=$categories.Body[0].id; description="Manual $stamp"; amount=10; paymentMethod='Cash'; reference="MAN-$stamp" }
    $manualId = $manual.Body.id
    $manualOwnership = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""OdontologoAccountingEntryOwnerships"" WHERE ""AccountingEntryId"" = '$manualId' AND ""OdontologoId"" = '$odId';"
    $accList = Invoke-Api 'GET' '/api/accounting/entries' $odToken $null
    Expect ($manual.StatusCode -eq 200 -and $manualOwnership -eq 1 -and $accList.StatusCode -eq 200) 'Odontologia' 'Contabilidad endpoint + ownership' "ManualEntry=$manualId" "manual=$($manual.StatusCode), own=$manualOwnership, list=$($accList.StatusCode)"
    $tele = Invoke-Api 'POST' '/api/odontologia/telemedicina/sesiones' $odToken @{ patientId=$odPatientId; patientName=''; topic="Tele $stamp"; meetingLink='https://meet.local'; scheduledStartAt=(Get-Date).AddHours(1).ToUniversalTime().ToString('o'); scheduledEndAt=(Get-Date).AddHours(2).ToUniversalTime().ToString('o') }
    $teleId = $tele.Body.id
    $teleMsg = Invoke-Api 'POST' "/api/odontologia/telemedicina/sesiones/$teleId/mensajes" $odToken @{ message='Mensaje auditoría' }
    $teleDb = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""TelemedicineMessages"" WHERE ""SessionId"" = '$teleId';"
    Expect ($tele.StatusCode -eq 200 -and $teleMsg.StatusCode -eq 200 -and $teleDb -ge 1) 'Odontologia' 'Telemedicina chat' "Session=$teleId" "tele=$($tele.StatusCode), msg=$($teleMsg.StatusCode), db=$teleDb"

    $portalPref = Invoke-Api 'PUT' "/api/odontologia/portal-paciente/pacientes/$odPatientId/preferencias" $odToken @{ emailEnabled=$true; whatsAppEnabled=$true }
    $portalReminder = Invoke-Api 'POST' "/api/odontologia/portal-paciente/pacientes/$odPatientId/recordatorios" $odToken @{ message='Recordatorio'; subject='Recordatorio'; sendEmail=$true; sendWhatsApp=$true }
    $portalNotifs = Invoke-Api 'GET' "/api/odontologia/portal-paciente/notificaciones?patientId=$odPatientId" $odToken $null
    $portalDb = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""PatientPortalNotifications"" WHERE ""PatientId"" = '$odPatientId';"
    Expect ($portalPref.StatusCode -eq 200 -and $portalReminder.StatusCode -eq 200 -and $portalNotifs.StatusCode -eq 200 -and $portalDb -ge 1) 'Odontologia' 'Portal paciente y notificaciones' "Notifs=$portalDb" "pref=$($portalPref.StatusCode), reminder=$($portalReminder.StatusCode), list=$($portalNotifs.StatusCode), db=$portalDb"

    $coverage = Invoke-Api 'POST' '/api/odontologia/seguros/validar-cobertura' $odToken @{ patientId=$odPatientId; insurerName='IESS'; policyNumber="POL-$stamp"; procedureCode='D1110'; requestedAmount=50 }
    $claim = Invoke-Api 'POST' '/api/odontologia/seguros/reclamaciones' $odToken @{ patientId=$odPatientId; insurerName='IESS'; policyNumber="POL-$stamp"; procedureCode='D1110'; procedureDescription='Profilaxis'; requestedAmount=50 }
    $claimId = $claim.Body.id
    $claimUpd = Invoke-Api 'PUT' "/api/odontologia/seguros/reclamaciones/$claimId/estado" $odToken @{ status='Approved'; approvedAmount=35; responseMessage='Aprobado' }
    $claimStatus = Sql-Scalar 'medicsys_odontologia' "SELECT ""Status""::text FROM ""InsuranceClaims"" WHERE ""Id"" = '$claimId';"
    Expect ($coverage.StatusCode -eq 200 -and $claim.StatusCode -eq 200 -and $claimUpd.StatusCode -eq 200 -and ($claimStatus -eq 'Approved' -or $claimStatus -eq '1')) 'Odontologia' 'Seguros reclamos' "Claim=$claimId" "coverage=$($coverage.StatusCode), claim=$($claim.StatusCode), upd=$($claimUpd.StatusCode), status=$claimStatus"

    $doc = Invoke-Api 'POST' '/api/odontologia/documentos-firmados' $odToken @{ patientId=$odPatientId; documentType='Receta'; documentName="Receta $stamp"; signatureProvider='FirmaEC'; signatureSerial="SER-$stamp"; documentContent='Ibuprofeno'; notes='Audit' }
    $docId = $doc.Body.id
    $docDb = Sql-Count 'medicsys_odontologia' "SELECT COUNT(*) FROM ""SignedClinicalDocuments"" WHERE ""Id"" = '$docId';"
    Expect ($doc.StatusCode -eq 200 -and $docDb -eq 1) 'Odontologia' 'Documentos firmados' "Doc=$docId" "doc=$($doc.StatusCode), db=$docDb"

    $financial = Invoke-Api 'GET' '/api/odontologia/reportes/financiero' $odToken $null
    $sales = Invoke-Api 'GET' '/api/odontologia/reportes/ventas' $odToken $null
    $comparative = Invoke-Api 'GET' '/api/odontologia/reportes/comparativo?months=6' $odToken $null
    $advanced = Invoke-Api 'GET' '/api/odontologia/reportes/avanzado' $odToken $null
    $incomeDb = To-Dec (Sql-Scalar 'medicsys_odontologia' "SELECT COALESCE(SUM(i.""TotalToCharge""),0) FROM ""Invoices"" i INNER JOIN ""OdontologoInvoiceOwnerships"" o ON o.""InvoiceId"" = i.""Id"" WHERE o.""OdontologoId"" = '$odId' AND i.""Status"" <> 2;")
    $incomeApi = To-Dec $financial.Body.summary.totalIncome
    $incomeMatch = [math]::Abs([double]($incomeApi - $incomeDb)) -lt 0.01
    Expect ($financial.StatusCode -eq 200 -and $sales.StatusCode -eq 200 -and $comparative.StatusCode -eq 200 -and $advanced.StatusCode -eq 200 -and $incomeMatch) 'Reportes' 'Reportes desde BD' "incomeApi=$incomeApi incomeDb=$incomeDb" "financial=$($financial.StatusCode), sales=$($sales.StatusCode), comparative=$($comparative.StatusCode), advanced=$($advanced.StatusCode), match=$incomeMatch"

    $sriPending = Invoke-Api 'GET' '/api/sri/pending-invoices' $odToken $null
    $sriStats = Invoke-Api 'GET' '/api/sri/stats' $odToken $null
    Expect ($sriPending.StatusCode -eq 200 -and $sriStats.StatusCode -eq 200) 'SRI' 'Consultas SRI' 'OK' "pending=$($sriPending.StatusCode), stats=$($sriStats.StatusCode)"

    $aiNotes = Invoke-Api 'POST' '/api/ai/suggest-notes' $odToken @{ reason='Dolor'; currentIssue='Sensibilidad'; plan='Control'; procedures='Limpieza' }
    $aiDiag = Invoke-Api 'POST' '/api/ai/suggest-diagnosis' $odToken @{ symptoms='dolor dental con frío'; clinicalFindings='caries en molar'; notes='sin fiebre' }
    $aiPred = Invoke-Api 'GET' '/api/ai/predictive-trends?months=6' $odToken $null
    Expect ($aiNotes.StatusCode -eq 200 -and $aiNotes.Body.suggestion -and $aiDiag.StatusCode -eq 200 -and $aiDiag.Body.primarySuggestion.diagnosis -and $aiPred.StatusCode -eq 200) 'IA' 'Endpoints IA' 'OK' "notes=$($aiNotes.StatusCode), diag=$($aiDiag.StatusCode), pred=$($aiPred.StatusCode)"

    $mockScan = (rg -n --glob "MEDICSYS.Api/**" --glob "MEDICSYS.Web/src/**" "mock|hardcoded|simulaci[oó]n|dummy|fake|en desarrollo|Funcionalidad de exportación" | Out-String)
    $hasMock = $mockScan -match 'simulaci|Funcionalidad de exportación|mock|hardcoded|dummy|fake'
    $mockDetail = if ($mockScan.Length -gt 800) { $mockScan.Substring(0,800) } else { $mockScan }
    Expect (-not $hasMock) 'AuditoriaCodigo' 'Patrones mock/simulación' 'Sin patrones' $mockDetail

    Set-Location $projectRoot
    $apiBuild = (& $dotnet build MEDICSYS.Api/MEDICSYS.Api.csproj 2>&1 | Out-String)
    $apiExit = $LASTEXITCODE
    $webLogPath = Join-Path $projectRoot 'MEDICSYS.Web\build-audit.log'
    cmd /c "cd /d `"$projectRoot\MEDICSYS.Web`" && npm run build > `"$webLogPath`" 2>&1"
    $webExit = $LASTEXITCODE
    $webBuild = if (Test-Path $webLogPath) { Get-Content $webLogPath -Raw } else { '' }
    $apiBuildFailDetail = if ($apiBuild.Length -gt 400) { $apiBuild.Substring(0,400) } else { $apiBuild }
    Expect ($apiExit -eq 0) 'Build' 'Backend build' 'OK' "Falló backend build (exit=$apiExit): $apiBuildFailDetail"
    Expect ($webExit -eq 0 -and $webBuild -match 'Application bundle generation complete') 'Build' 'Frontend build' 'OK' "Falló frontend build (exit=$webExit)"

    $passed = ($results | Where-Object { $_.Pass }).Count
    $failed = ($results | Where-Object { -not $_.Pass }).Count
    $reportPath = Join-Path $projectRoot "docs/desarrollo/VERIFICACION_INTEGRAL_$stamp.json"
    [PSCustomObject]@{ TimestampUtc=(Get-Date).ToUniversalTime().ToString('o'); Passed=$passed; Failed=$failed; Total=$results.Count; Results=$results } | ConvertTo-Json -Depth 20 | Set-Content $reportPath

    Write-Host "REPORTE_GENERADO=$reportPath"
    Write-Host "RESULTADOS_PASS=$passed"
    Write-Host "RESULTADOS_FAIL=$failed"
    $results | Sort-Object Layer, Test | Format-Table Layer, Test, Pass, Details -AutoSize
}
finally {
    Stop-Job -Job $apiJob -ErrorAction SilentlyContinue | Out-Null
    Receive-Job -Job $apiJob -Keep -ErrorAction SilentlyContinue | Out-Null
    Remove-Job -Job $apiJob -Force -ErrorAction SilentlyContinue | Out-Null
}
