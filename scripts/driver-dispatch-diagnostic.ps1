param(
    [string]$BaseUrl = "http://127.0.0.1:8282",
    [string]$AdminEmail = "admin@ridepr.test",
    [string]$AdminPassword = "Senha123!"
)

$ErrorActionPreference = "Stop"
$steps = New-Object System.Collections.Generic.List[object]

function Add-Step($Name, $Success, $Endpoint, $Status, $Details = "") {
    $steps.Add([pscustomobject]@{
        step = $Name
        success = $Success
        endpoint = $Endpoint
        status = $Status
        details = $Details
    })
}

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

function Write-Report($Summary) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Driver dispatch diagnostic report")
    $lines.Add("")
    $lines.Add("Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lines.Add("")
    $lines.Add("## Resumo")
    $lines.Add("")
    $lines.Add("- DriverId: $($Summary.driverId)")
    $lines.Add("- PassengerId: $($Summary.passengerId)")
    $lines.Add("- VehicleId: $($Summary.vehicleId)")
    $lines.Add("- TripId: $($Summary.tripId)")
    $lines.Add("- OfferDriverId: $($Summary.offerDriverId)")
    $lines.Add("- FinalTripStatus: $($Summary.finalTripStatus)")
    $lines.Add("")
    $lines.Add("## Etapas")
    $lines.Add("")

    foreach ($step in $steps) {
        $ok = if ($step.success) { "OK" } else { "FAIL" }
        $lines.Add("- [$ok] $($step.step) | $($step.endpoint) | $($step.status) | $($step.details)")
    }

    $lines.Add("")
    $lines.Add("## Observacao")
    $lines.Add("")
    $lines.Add("Este diagnostico comprova login, DriverId, aprovacao, veiculo ativo, online no backend, heartbeat/localizacao, live-drivers, disparo de oferta de teste, dispatch real e aceite. A confirmacao visual do popup deve ser vista no app-driver aberto, pelos logs DISPATCH_EVENT_RECEIVED e OFFER_DIALOG_OPENED.")

    Set-Content -Path "DRIVER_DISPATCH_DIAGNOSTIC_REPORT.md" -Value $lines
}

try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health"
    Add-Step "HEALTH" $true "/health" "200" $health

    $stamp = Get-Date -Format "HHmmssfff"
    $admin = Invoke-JsonPost "/api/auth/login" @{
        email = $AdminEmail
        password = $AdminPassword
    }
    $adminHeaders = @{ Authorization = "Bearer $($admin.accessToken)" }
    Add-Step "ADMIN_LOGIN" $true "/api/auth/login" "200" $admin.userId

    $passengerEmail = "diag.pass.$stamp@ridepr.test"
    $driverEmail = "diag.driver.$stamp@ridepr.test"
    $password = "Senha123!"

    Invoke-JsonPost "/api/auth/register" @{
        name = "Diag Passageiro $stamp"
        email = $passengerEmail
        password = $password
        role = 1
    } | Out-Null
    Add-Step "PASSENGER_REGISTER" $true "/api/auth/register" "200" $passengerEmail

    Invoke-JsonPost "/api/auth/register" @{
        name = "Diag Motorista $stamp"
        email = $driverEmail
        password = $password
        role = 2
    } | Out-Null
    Add-Step "DRIVER_REGISTER" $true "/api/auth/register" "200" $driverEmail

    $passenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $adminHeaders) |
        Where-Object { $_.email -eq $passengerEmail } |
        Select-Object -First 1
    $driver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $adminHeaders) |
        Where-Object { $_.email -eq $driverEmail } |
        Select-Object -First 1

    if (-not $passenger) { throw "Passageiro nao encontrado no admin." }
    if (-not $driver) { throw "Motorista nao encontrado no admin." }
    Add-Step "DRIVER_PROFILE_LOADED" $true "/api/admin/drivers" "200" $driver.id

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/drivers/$($driver.id)/approve" -Headers $adminHeaders | Out-Null
    Add-Step "DRIVER_APPROVED" $true "/api/admin/drivers/$($driver.id)/approve" "200" "Approved"

    $vehicle = Invoke-JsonPost "/api/admin/vehicles" @{
        driverId = $driver.id
        plate = "DGD$($stamp.Substring($stamp.Length - 4))"
        brand = "RidePR"
        model = "Diagnostico"
        year = 2026
        color = "Prata"
        renavam = ""
        chassis = ""
        active = $true
    } $adminHeaders
    Add-Step "VEHICLE_CREATED" $true "/api/admin/vehicles" "200" $vehicle.id

    $driverLogin = Invoke-JsonPost "/api/auth/login" @{
        email = $driverEmail
        password = $password
    }
    $driverHeaders = @{ Authorization = "Bearer $($driverLogin.accessToken)" }
    Add-Step "DRIVER_LOGIN" $true "/api/auth/login" "200" $driverLogin.userId

    Invoke-JsonPatch "/api/drivers/$($driver.id)/status" @{ status = 2 } $driverHeaders | Out-Null
    Add-Step "DRIVER_ONLINE_CONFIRMED" $true "/api/drivers/$($driver.id)/status" "200" "Online"

    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/driver-location?driverId=$($driver.id)&latitude=-25.4284&longitude=-49.2733&speed=10&heading=80" `
        -Headers $driverHeaders | Out-Null
    Add-Step "HEARTBEAT_LOCATION_SENT" $true "/api/driver-location" "200" "lat=-25.4284 lng=-49.2733"

    $liveDrivers = Invoke-RestMethod -Uri "$BaseUrl/api/admin/live-drivers?onlineOnly=true&limit=500" -Headers $adminHeaders
    $liveDriver = ($liveDrivers | Where-Object { $_.driverId -eq $driver.id } | Select-Object -First 1)
    if (-not $liveDriver) { throw "Motorista online nao apareceu em live-drivers." }
    Add-Step "LIVE_DRIVERS_CONFIRMED" $true "/api/admin/live-drivers" "200" $driver.id

    $testOffer = Invoke-JsonPost "/api/debug/drivers/$($driver.id)/test-offer" @{} $adminHeaders
    Add-Step "DEBUG_TEST_OFFER_SENT" $true "/api/debug/drivers/$($driver.id)/test-offer" "200" $testOffer.tripId

    $trip = Invoke-JsonPost "/api/trips" @{
        passengerId = $passenger.id
        origin = "Origem diagnostico"
        destination = "Destino diagnostico"
        originLatitude = -25.4286
        originLongitude = -49.2731
        destinationLatitude = -25.4372
        destinationLongitude = -49.2699
    } $adminHeaders
    Add-Step "TRIP_CREATED" $true "/api/trips" "200" $trip.id

    $dispatch = Invoke-JsonPost "/api/dispatch/request" @{
        tripId = $trip.id
        radiusKm = 10
        timeoutSeconds = 60
        maxCandidates = 5
    } $adminHeaders
    Add-Step "DISPATCH_STARTED" $true "/api/dispatch/request" "200" "candidates=$($dispatch.candidates.Count)"

    if (-not $dispatch.currentOffer) { throw "Dispatch nao criou oferta." }
    if ($dispatch.currentOffer.driverId -ne $driver.id) { throw "Oferta foi para outro motorista: $($dispatch.currentOffer.driverId)" }
    Add-Step "OFFER_CREATED" $true "/api/dispatch/request" "200" "driverId=$($dispatch.currentOffer.driverId)"

    $acceptedTrip = Invoke-JsonPost "/api/dispatch/$($trip.id)/accept" @{
        driverId = $driver.id
        reason = ""
    } $driverHeaders
    Add-Step "OFFER_ACCEPTED" $true "/api/dispatch/$($trip.id)/accept" "200" $acceptedTrip.status

    $summary = [pscustomobject]@{
        driverId = $driver.id
        passengerId = $passenger.id
        vehicleId = $vehicle.id
        tripId = $trip.id
        offerDriverId = $dispatch.currentOffer.driverId
        finalTripStatus = $acceptedTrip.status
    }

    Write-Report $summary
    $summary | ConvertTo-Json
} catch {
    Add-Step "ERROR" $false "" "FAILED" $_.Exception.Message
    Write-Report ([pscustomobject]@{
        driverId = ""
        passengerId = ""
        vehicleId = ""
        tripId = ""
        offerDriverId = ""
        finalTripStatus = "FAILED"
    })
    throw
}
