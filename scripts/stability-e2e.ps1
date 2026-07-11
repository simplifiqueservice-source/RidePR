param(
    [string]$BaseUrl = "http://127.0.0.1:8282",
    [string]$AdminEmail = "admin@ridepr.test",
    [string]$AdminPassword = "Senha123!",
    [int]$PresenceWaitSeconds = 65
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost($Path, $Body, $Headers = $null) {
    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl$Path" `
        -ContentType "application/json" `
        -Headers $Headers `
        -Body ($Body | ConvertTo-Json -Depth 8)
}

function Invoke-JsonPatch($Path, $Body, $Headers = $null) {
    Invoke-RestMethod `
        -Method Patch `
        -Uri "$BaseUrl$Path" `
        -ContentType "application/json" `
        -Headers $Headers `
        -Body ($Body | ConvertTo-Json -Depth 8)
}

function Write-Report($Result) {
    $lines = @(
        "# RidePR stability E2E report",
        "",
        "Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
        "",
        "## Resultado",
        "",
        "- Health: $($Result.health)",
        "- Passageiro criado/aprovado: $($Result.passengerAutoApproved)",
        "- Motorista inicial: $($Result.driverInitialApproval)",
        "- Motorista apos aprovacao admin: $($Result.driverAdminApproval)",
        "- Motorista online apareceu no painel: $($Result.onlineVisible)",
        "- Motorista expirado saiu do painel: $($Result.expiredRemoved)",
        "- Exclusao passageiro: $($Result.deletePassenger)",
        "- Exclusao motorista: $($Result.deleteDriver)",
        "",
        "## IDs",
        "",
        "- Passageiro: $($Result.passengerId)",
        "- Motorista: $($Result.driverId)",
        "- Veiculo: $($Result.vehicleId)",
        "",
        "## Observacao",
        "",
        "Este teste cobre presenca, aprovacao, localizacao e limpeza de motorista fantasma. Fluxos visuais, rotas completas, som/vibracao e APKs devem ser validados em etapas complementares."
    )

    Set-Content -Path "STABILITY_E2E_REPORT.md" -Value $lines
}

$health = Invoke-RestMethod -Uri "$BaseUrl/health"

$stamp = Get-Date -Format "HHmmssfff"
$admin = Invoke-JsonPost "/api/auth/login" @{
    email = $AdminEmail
    password = $AdminPassword
}
$adminHeaders = @{ Authorization = "Bearer $($admin.accessToken)" }

$passengerEmail = "e2e.pass.$stamp@ridepr.test"
$driverEmail = "e2e.driver.$stamp@ridepr.test"
$driverPassword = "Senha123!"

Invoke-JsonPost "/api/auth/register" @{
    name = "E2E Passageiro $stamp"
    email = $passengerEmail
    password = "Senha123!"
    role = 1
} | Out-Null

Invoke-JsonPost "/api/auth/register" @{
    name = "E2E Motorista $stamp"
    email = $driverEmail
    password = $driverPassword
    role = 2
} | Out-Null

$passenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $adminHeaders) |
    Where-Object { $_.email -eq $passengerEmail } |
    Select-Object -First 1
$driver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $adminHeaders) |
    Where-Object { $_.email -eq $driverEmail } |
    Select-Object -First 1

if (-not $passenger) { throw "Passageiro E2E nao apareceu no admin." }
if (-not $driver) { throw "Motorista E2E nao apareceu no admin." }
if (-not $passenger.active) { throw "Passageiro deveria nascer ativo." }
if ($driver.approvalStatus -ne "Pending") { throw "Motorista deveria nascer pendente." }

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/drivers/$($driver.id)/approve" -Headers $adminHeaders | Out-Null

$vehicle = Invoke-JsonPost "/api/admin/vehicles" @{
    driverId = $driver.id
    plate = "E2E$($stamp.Substring($stamp.Length - 4))"
    brand = "RidePR"
    model = "Teste"
    year = 2026
    color = "Prata"
    renavam = ""
    chassis = ""
    active = $true
} $adminHeaders

$driverLogin = Invoke-JsonPost "/api/auth/login" @{
    email = $driverEmail
    password = $driverPassword
}
$driverHeaders = @{ Authorization = "Bearer $($driverLogin.accessToken)" }

Invoke-JsonPatch "/api/drivers/$($driver.id)/status" @{ status = 2 } $driverHeaders | Out-Null

Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/driver-location?driverId=$($driver.id)&latitude=-25.4284&longitude=-49.2733&speed=12&heading=90" `
    -Headers $driverHeaders | Out-Null

$liveDrivers = Invoke-RestMethod -Uri "$BaseUrl/api/admin/live-drivers?onlineOnly=true&limit=500" -Headers $adminHeaders
$onlineVisible = [bool](($liveDrivers | Where-Object { $_.driverId -eq $driver.id }) | Select-Object -First 1)

if (-not $onlineVisible) { throw "Motorista online nao apareceu no painel." }

Start-Sleep -Seconds $PresenceWaitSeconds

$liveAfterExpire = Invoke-RestMethod -Uri "$BaseUrl/api/admin/live-drivers?onlineOnly=true&limit=500" -Headers $adminHeaders
$stillOnline = [bool](($liveAfterExpire | Where-Object { $_.driverId -eq $driver.id }) | Select-Object -First 1)

if ($stillOnline) { throw "Motorista expirado continuou online no painel." }

$deletePassenger = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/passengers/$($passenger.id)" -Headers $adminHeaders
$deleteDriver = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/drivers/$($driver.id)" -Headers $adminHeaders

$result = [pscustomobject]@{
    health = $health
    passengerId = $passenger.id
    driverId = $driver.id
    vehicleId = $vehicle.id
    passengerAutoApproved = $passenger.active
    driverInitialApproval = $driver.approvalStatus
    driverAdminApproval = "Approved"
    onlineVisible = $onlineVisible
    expiredRemoved = -not $stillOnline
    deletePassenger = $deletePassenger.action
    deleteDriver = $deleteDriver.action
}

Write-Report $result
$result | ConvertTo-Json
