param(
    [string]$BaseUrl = "http://127.0.0.1:8282",
    [string]$AdminEmail = "admin@ridepr.test",
    [string]$AdminPassword = "Senha123!"
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost($Path, $Body, $Headers = $null) {
    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl$Path" `
        -ContentType "application/json" `
        -Headers $Headers `
        -Body ($Body | ConvertTo-Json)
}

$stamp = Get-Date -Format "HHmmssfff"
$admin = Invoke-JsonPost "/api/auth/login" @{
    email = $AdminEmail
    password = $AdminPassword
}
$headers = @{ Authorization = "Bearer $($admin.accessToken)" }

$passEmail = "smoke.pass.$stamp@ridepr.test"
$driverEmail = "smoke.driver.$stamp@ridepr.test"

Invoke-JsonPost "/api/auth/register" @{
    name = "Smoke Passageiro $stamp"
    email = $passEmail
    password = "Senha123!"
    role = 1
} | Out-Null

Invoke-JsonPost "/api/auth/register" @{
    name = "Smoke Motorista $stamp"
    email = $driverEmail
    password = "Senha123!"
    role = 2
} | Out-Null

$passenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $headers) |
    Where-Object { $_.email -eq $passEmail } |
    Select-Object -First 1
$driver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $headers) |
    Where-Object { $_.email -eq $driverEmail } |
    Select-Object -First 1

if (-not $passenger) { throw "Passageiro registrado sem perfil admin." }
if (-not $driver) { throw "Motorista registrado sem perfil admin." }
if (-not $passenger.active) { throw "Passageiro registrado deveria nascer ativo/aprovado." }
if ($driver.approvalStatus -ne "Pending") { throw "Motorista registrado deveria nascer pendente de aprovacao." }

$approvedDriver = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/drivers/$($driver.id)/approve" -Headers $headers
$driverAfterApproval = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $headers) |
    Where-Object { $_.email -eq $driverEmail } |
    Select-Object -First 1

if ($driverAfterApproval.approvalStatus -ne "Approved") { throw "Motorista nao foi aprovado pelo endpoint admin." }

$adminPassEmail = "admin.pass.$stamp@ridepr.test"
$adminPassengerCreated = Invoke-JsonPost "/api/admin/passengers" @{
    name = "Admin Passageiro $stamp"
    email = $adminPassEmail
    password = "Senha123!"
    cpf = "AP$stamp"
    birthDate = "1990-01-01"
    phone = "41999990000"
    emergencyPhone = ""
    address = "Rua Smoke"
    city = "Curitiba"
    state = "PR"
    zipCode = "80000000"
    branchId = $null
    active = $false
} $headers

$adminDriverEmail = "admin.driver.$stamp@ridepr.test"
$adminDriverCreated = Invoke-JsonPost "/api/admin/drivers" @{
    name = "Admin Motorista $stamp"
    email = $adminDriverEmail
    password = "Senha123!"
    cpf = "AD$stamp"
    rg = "RG$stamp"
    birthDate = "1990-01-01"
    phone = "41999991111"
    emergencyPhone = ""
    address = "Rua Smoke"
    city = "Curitiba"
    state = "PR"
    zipCode = "80000000"
    branchId = $null
    cnhNumber = "CNH$stamp"
    cnhCategory = "B"
    cnhExpiration = "2030-01-01"
    approved = $true
    active = $true
} $headers

$adminPassenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $headers) |
    Where-Object { $_.email -eq $adminPassEmail } |
    Select-Object -First 1
$adminDriver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $headers) |
    Where-Object { $_.email -eq $adminDriverEmail } |
    Select-Object -First 1

if (-not $adminPassenger.active) { throw "Passageiro criado pelo admin deveria nascer ativo/aprovado." }
if ($adminDriver.approvalStatus -ne "Pending") { throw "Motorista criado pelo admin deveria nascer pendente mesmo com approved=true." }

$deletePassenger = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/passengers/$($passenger.id)" -Headers $headers
$deleteDriver = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/drivers/$($driver.id)" -Headers $headers
$deleteAdminPassenger = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/passengers/$($adminPassenger.id)" -Headers $headers
$deleteAdminDriver = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/drivers/$($adminDriver.id)" -Headers $headers

$softPassEmail = "soft.pass.$stamp@ridepr.test"
Invoke-JsonPost "/api/auth/register" @{
    name = "Smoke Passageiro Soft $stamp"
    email = $softPassEmail
    password = "Senha123!"
    role = 1
} | Out-Null

$softPassenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $headers) |
    Where-Object { $_.email -eq $softPassEmail } |
    Select-Object -First 1

$trip = Invoke-JsonPost "/api/trips" @{
    passengerId = $softPassenger.id
    origin = "Origem smoke"
    destination = "Destino smoke"
    originLatitude = -23.55052
    originLongitude = -46.63331
    destinationLatitude = -23.56141
    destinationLongitude = -46.65588
} $headers

$softDeletePassenger = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/passengers/$($softPassenger.id)" -Headers $headers

$softDriverEmail = "soft.driver.$stamp@ridepr.test"
Invoke-JsonPost "/api/auth/register" @{
    name = "Smoke Motorista Soft $stamp"
    email = $softDriverEmail
    password = "Senha123!"
    role = 2
} | Out-Null

$softDriver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $headers) |
    Where-Object { $_.email -eq $softDriverEmail } |
    Select-Object -First 1

$vehicle = Invoke-JsonPost "/api/admin/vehicles" @{
    driverId = $softDriver.id
    plate = "SMK$($stamp.Substring($stamp.Length - 4))"
    brand = "Smoke"
    model = "Teste"
    year = 2026
    color = "Prata"
    renavam = ""
    chassis = ""
    active = $true
} $headers

$softDeleteDriver = Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/drivers/$($softDriver.id)" -Headers $headers

[pscustomobject]@{
    registeredPassengerProfile = $passenger.id
    registeredDriverProfile = $driver.id
    passengerAutoApproved = $passenger.active
    driverInitialApproval = $driver.approvalStatus
    driverAdminApproval = $driverAfterApproval.approvalStatus
    adminPassengerAutoApproved = $adminPassenger.active
    adminDriverInitialApproval = $adminDriver.approvalStatus
    deletePassenger = $deletePassenger.action
    deleteDriver = $deleteDriver.action
    deleteAdminPassenger = $deleteAdminPassenger.action
    deleteAdminDriver = $deleteAdminDriver.action
    softDeletePassenger = $softDeletePassenger.action
    softDeleteDriver = $softDeleteDriver.action
    tripCreated = $trip.id
    vehicleCreated = $vehicle.id
} | ConvertTo-Json
