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
    $lines.Add("# Online location dispatch report")
    $lines.Add("")
    $lines.Add("Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lines.Add("")
    $lines.Add("## Resumo")
    $lines.Add("")
    $lines.Add("- DriverId: $($Summary.driverId)")
    $lines.Add("- PassengerId: $($Summary.passengerId)")
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

    Set-Content -Path "ONLINE_LOCATION_DISPATCH_REPORT.md" -Value $lines
}

try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health"
    Add-Step "HEALTH" $true "/health" "200" $health

    $stamp = Get-Date -Format "HHmmssfff"
    $admin = Invoke-JsonPost "/api/auth/login" @{ email = $AdminEmail; password = $AdminPassword }
    $adminHeaders = @{ Authorization = "Bearer $($admin.accessToken)" }
    Add-Step "ADMIN_LOGIN" $true "/api/auth/login" "200" $admin.userId

    $passengerEmail = "online.pass.$stamp@ridepr.test"
    $driverEmail = "online.driver.$stamp@ridepr.test"
    $password = "Senha123!"

    Invoke-JsonPost "/api/auth/register" @{ name = "Online Passageiro $stamp"; email = $passengerEmail; password = $password; role = 1 } | Out-Null
    Invoke-JsonPost "/api/auth/register" @{ name = "Online Motorista $stamp"; email = $driverEmail; password = $password; role = 2 } | Out-Null
    Add-Step "USERS_CREATED" $true "/api/auth/register" "200" "$passengerEmail / $driverEmail"

    $passenger = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/passengers" -Headers $adminHeaders) |
        Where-Object { $_.email -eq $passengerEmail } |
        Select-Object -First 1
    $driver = (Invoke-RestMethod -Uri "$BaseUrl/api/admin/drivers" -Headers $adminHeaders) |
        Where-Object { $_.email -eq $driverEmail } |
        Select-Object -First 1

    if (-not $passenger) { throw "Passageiro nao encontrado." }
    if (-not $driver) { throw "Motorista nao encontrado." }
    Add-Step "DRIVER_ID_RESOLVED" $true "/api/admin/drivers" "200" $driver.id

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/drivers/$($driver.id)/approve" -Headers $adminHeaders | Out-Null
    Add-Step "DRIVER_APPROVED" $true "/api/admin/drivers/$($driver.id)/approve" "200" "Approved"

    $vehicle = Invoke-JsonPost "/api/admin/vehicles" @{
        driverId = $driver.id
        plate = "OLD$($stamp.Substring($stamp.Length - 4))"
        brand = "RidePR"
        model = "Online"
        year = 2026
        color = "Prata"
        renavam = ""
        chassis = ""
        active = $true
    } $adminHeaders
    Add-Step "ACTIVE_VEHICLE_CREATED" $true "/api/admin/vehicles" "200" $vehicle.id

    $driverLogin = Invoke-JsonPost "/api/auth/login" @{ email = $driverEmail; password = $password }
    $driverHeaders = @{ Authorization = "Bearer $($driverLogin.accessToken)" }
    Add-Step "DRIVER_LOGIN" $true "/api/auth/login" "200" $driverLogin.userId

    Invoke-JsonPatch "/api/drivers/$($driver.id)/status" @{ status = 2 } $driverHeaders | Out-Null
    Add-Step "DRIVER_ONLINE_STATUS" $true "/api/drivers/$($driver.id)/status" "200" "Online"

    $location = Invoke-JsonPost "/api/drivers/me/location" @{
        latitude = -25.4284
        longitude = -49.2733
        accuracy = 8
        speed = 3
        heading = 90
        recordedAt = (Get-Date).ToUniversalTime().ToString("o")
    } $driverHeaders
    Add-Step "ME_LOCATION_SENT" $true "/api/drivers/me/location" "200" "$($location.latitude),$($location.longitude)"

    $heartbeat = Invoke-JsonPost "/api/drivers/me/heartbeat" @{} $driverHeaders
    Add-Step "ME_HEARTBEAT_SENT" $true "/api/drivers/me/heartbeat" "200" $heartbeat.lastHeartbeatAt

    $liveDrivers = Invoke-RestMethod -Uri "$BaseUrl/api/admin/live-drivers?onlineOnly=true&limit=500" -Headers $adminHeaders
    $liveDriver = ($liveDrivers | Where-Object { $_.driverId -eq $driver.id } | Select-Object -First 1)
    if (-not $liveDriver) { throw "Motorista online nao apareceu em live-drivers." }
    Add-Step "LIVE_DRIVERS_VISIBLE" $true "/api/admin/live-drivers" "200" "$($liveDriver.latitude),$($liveDriver.longitude)"

    $nearby = Invoke-RestMethod -Uri "$BaseUrl/api/driver-location/nearby?latitude=-25.4284&longitude=-49.2733&radiusKm=10" -Headers $driverHeaders
    $nearbyDriver = ($nearby | Where-Object { $_.driverId -eq $driver.id } | Select-Object -First 1)
    if (-not $nearbyDriver) { throw "Motorista nao apareceu em nearby." }
    Add-Step "NEARBY_VISIBLE" $true "/api/driver-location/nearby" "200" $nearbyDriver.driverId

    Invoke-JsonPost "/api/debug/drivers/$($driver.id)/test-offer" @{} $adminHeaders | Out-Null
    Add-Step "TEST_OFFER_SENT" $true "/api/debug/drivers/$($driver.id)/test-offer" "200" "DispatchOfferReceived"

    $trip = Invoke-JsonPost "/api/trips" @{
        passengerId = $passenger.id
        origin = "Origem online"
        destination = "Destino online"
        originLatitude = -25.4286
        originLongitude = -49.2731
        destinationLatitude = -25.4372
        destinationLongitude = -49.2699
    } $adminHeaders
    Add-Step "TRIP_CREATED" $true "/api/trips" "200" $trip.id

    $dispatch = Invoke-JsonPost "/api/dispatch/request" @{ tripId = $trip.id; radiusKm = 10; timeoutSeconds = 60; maxCandidates = 5 } $adminHeaders
    if (-not $dispatch.currentOffer) { throw "Dispatch nao criou oferta." }
    Add-Step "OFFER_CREATED" $true "/api/dispatch/request" "200" $dispatch.currentOffer.driverId

    $accepted = Invoke-JsonPost "/api/dispatch/$($trip.id)/accept" @{ driverId = $driver.id; reason = "" } $driverHeaders
    Add-Step "OFFER_ACCEPTED" $true "/api/dispatch/$($trip.id)/accept" "200" $accepted.status

    $summary = [pscustomobject]@{
        driverId = $driver.id
        passengerId = $passenger.id
        tripId = $trip.id
        offerDriverId = $dispatch.currentOffer.driverId
        finalTripStatus = $accepted.status
    }
    Write-Report $summary
    $summary | ConvertTo-Json
} catch {
    Add-Step "ERROR" $false "" "FAILED" $_.Exception.Message
    Write-Report ([pscustomobject]@{ driverId = ""; passengerId = ""; tripId = ""; offerDriverId = ""; finalTripStatus = "FAILED" })
    throw
}
