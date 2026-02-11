# ==============================================================================
# DT-Express TMS - API Verification Script
# ==============================================================================
# Tests all 21 endpoints against a running API instance.
# Usage: .\scripts\verify-api.ps1 [-BaseUrl "http://localhost:5198"]
# ==============================================================================

param(
    [string]$BaseUrl = "http://localhost:5198"
)

$ErrorActionPreference = "Continue"
$results = @()

# -- Helper functions ----------------------------------------------------------

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [string]$Token = $null,
        [int[]]$ExpectedStatus = @(200),
        [scriptblock]$Validate = $null
    )

    $headers = @{
        "Content-Type"     = "application/json"
        "X-Correlation-ID" = "verify-$(Get-Random)"
    }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    try {
        $params = @{
            Uri         = $Url
            Method      = $Method
            Headers     = $headers
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        if ($Body) {
            $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body)
        }

        $response  = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        $json       = $response.Content | ConvertFrom-Json

        if ($ExpectedStatus -contains $statusCode) {
            if ($Validate) {
                $vr = & $Validate $json
                if ($vr -eq $true) {
                    return @{ Name=$Name; Status="PASS"; Code=$statusCode; Message="OK" }
                } else {
                    return @{ Name=$Name; Status="FAIL"; Code=$statusCode; Message="Validation: $vr" }
                }
            }
            return @{ Name=$Name; Status="PASS"; Code=$statusCode; Message="OK" }
        } else {
            return @{ Name=$Name; Status="FAIL"; Code=$statusCode; Message="Expected $($ExpectedStatus -join '/'), got $statusCode" }
        }
    }
    catch {
        $er = $_.Exception.Response
        if ($er) {
            $sc = [int]$er.StatusCode
            if ($ExpectedStatus -contains $sc) {
                return @{ Name=$Name; Status="PASS"; Code=$sc; Message="OK (expected $sc)" }
            }
            return @{ Name=$Name; Status="FAIL"; Code=$sc; Message=$_.Exception.Message }
        }
        return @{ Name=$Name; Status="FAIL"; Code=0; Message=$_.Exception.Message }
    }
}

function Write-TestResult {
    param($R)
    $icon  = if ($R.Status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    $color = if ($R.Status -eq "PASS") { "Green"  } else { "Red"   }
    Write-Host ("  {0} {1} (HTTP {2}) - {3}" -f $icon, $R.Name, $R.Code, $R.Message) -ForegroundColor $color
}

function Invoke-Api {
    param([string]$Method, [string]$Url, [string]$Body=$null, [string]$Token=$null)
    $h = @{ "Content-Type"="application/json" }
    if ($Token) { $h["Authorization"] = "Bearer $Token" }
    $p = @{ Uri=$Url; Method=$Method; Headers=$h; ContentType="application/json"; ErrorAction="Stop" }
    if ($Body) { $p["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body) }
    $resp = Invoke-WebRequest @p
    return ($resp.Content | ConvertFrom-Json)
}

# -- Banner --------------------------------------------------------------------

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  DT-Express TMS - API Verification" -ForegroundColor Cyan
Write-Host "  Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "  Time:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ==============================================================================
# 1. AUTHENTICATION [3 endpoints]
# ==============================================================================
Write-Host '--- Authentication [3 endpoints] ---' -ForegroundColor Yellow

# #1 Login Admin
$loginBody = '{"username":"admin","password":"admin123"}'
$result = Test-Endpoint -Name '#1 POST /api/auth/login [Admin]' -Method POST `
    -Url "$BaseUrl/api/auth/login" -Body $loginBody -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.accessToken -and $j.data.role -eq "Admin") { $true } else { "Missing token or role" } }
Write-TestResult $result; $results += $result

# Extract tokens
$adminToken   = ""
$refreshToken = ""
if ($result.Status -eq "PASS") {
    $lr = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $loginBody
    $adminToken   = $lr.data.accessToken
    $refreshToken = $lr.data.refreshToken
}

# Login Dispatcher
$dispBody = '{"username":"dispatcher","password":"passwd123"}'
$result = Test-Endpoint -Name '#1b POST /api/auth/login [Dispatcher]' -Method POST `
    -Url "$BaseUrl/api/auth/login" -Body $dispBody -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.role -eq "Dispatcher") { $true } else { "Wrong role" } }
Write-TestResult $result; $results += $result

# Login Driver
$driverBody = '{"username":"driver","password":"passwd123"}'
$result = Test-Endpoint -Name '#1c POST /api/auth/login [Driver]' -Method POST `
    -Url "$BaseUrl/api/auth/login" -Body $driverBody -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.role -eq "Driver") { $true } else { "Wrong role" } }
Write-TestResult $result; $results += $result
$driverToken = ""
try { $dr = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $driverBody; $driverToken = $dr.data.accessToken } catch {}

# Login Viewer
$viewerBody = '{"username":"viewer","password":"passwd123"}'
$result = Test-Endpoint -Name '#1d POST /api/auth/login [Viewer]' -Method POST `
    -Url "$BaseUrl/api/auth/login" -Body $viewerBody -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.role -eq "Viewer") { $true } else { "Wrong role" } }
Write-TestResult $result; $results += $result
$viewerToken = ""
try { $vr = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $viewerBody; $viewerToken = $vr.data.accessToken } catch {}

# #2 Register
$rid = Get-Random
$regBody = '{"username":"verify_' + $rid + '","email":"verify_' + $rid + '@test.com","password":"testpass123","displayName":"TestUser","role":"Viewer"}'
$result = Test-Endpoint -Name '#2 POST /api/auth/register' -Method POST `
    -Url "$BaseUrl/api/auth/register" -Body $regBody -ExpectedStatus @(201) `
    -Validate { param($j) if ($j.success -and $j.data.accessToken) { $true } else { "No token" } }
Write-TestResult $result; $results += $result

# #3 Refresh
if ($refreshToken) {
    $refBody = '{"refreshToken":"' + $refreshToken + '"}'
    $result = Test-Endpoint -Name '#3 POST /api/auth/refresh' -Method POST `
        -Url "$BaseUrl/api/auth/refresh" -Body $refBody -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.accessToken) { $true } else { "No token" } }
    Write-TestResult $result; $results += $result
    # Re-login to get fresh admin token (refresh consumed the old one)
    try { $lr2 = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $loginBody; $adminToken = $lr2.data.accessToken } catch {}
} else {
    $result = @{ Name='#3 POST /api/auth/refresh'; Status="SKIP"; Code=0; Message="No refresh token" }
    Write-TestResult $result; $results += $result
}

Write-Host ""

# ==============================================================================
# 2. ORDERS [7 endpoints]
# ==============================================================================
Write-Host '--- Orders [7 endpoints] ---' -ForegroundColor Yellow

# #4 Create Order
$createOrderBody = '{"customerName":"Zhang San","customerPhone":"13812345678","customerEmail":"zhangsan@example.com","origin":{"street":"Lujiazui Ring Road 1000","city":"Shanghai","province":"Shanghai","postalCode":"200120","country":"CN"},"destination":{"street":"Huacheng Avenue 18","city":"Guangzhou","province":"Guangdong","postalCode":"510623","country":"CN"},"serviceLevel":"Express","items":[{"description":"Laptop computer","quantity":1,"weight":{"value":2.5,"unit":"Kg"},"dimensions":{"lengthCm":35.0,"widthCm":25.0,"heightCm":3.0}},{"description":"Charger and mouse","quantity":2,"weight":{"value":0.3,"unit":"Kg"}}]}'
$result = Test-Endpoint -Name '#4 POST /api/orders [Create]' -Method POST `
    -Url "$BaseUrl/api/orders" -Body $createOrderBody -Token $adminToken -ExpectedStatus @(201) `
    -Validate { param($j) if ($j.success -and $j.data.orderId -and $j.data.status -eq "Created") { $true } else { "Order not created" } }
Write-TestResult $result; $results += $result

# Extract orderId
$orderId = $null
if ($result.Status -eq "PASS") {
    try {
        $cr = Invoke-Api -Method POST -Url "$BaseUrl/api/orders" -Body $createOrderBody -Token $adminToken
        $orderId = $cr.data.orderId
    } catch {}
}

# #5 List Orders
$result = Test-Endpoint -Name '#5 GET /api/orders [List]' -Method GET `
    -Url "$BaseUrl/api/orders" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data -is [array]) { $true } else { "Not an array" } }
Write-TestResult $result; $results += $result

# #5b List with filter
$result = Test-Endpoint -Name '#5b GET /api/orders?status=Created' -Method GET `
    -Url "$BaseUrl/api/orders?status=Created" -Token $adminToken -ExpectedStatus @(200)
Write-TestResult $result; $results += $result

# #6 Get by ID
if ($orderId) {
    $result = Test-Endpoint -Name '#6 GET /api/orders/{id}' -Method GET `
        -Url "$BaseUrl/api/orders/$orderId" -Token $adminToken -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.id) { $true } else { "No data" } }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#6 GET /api/orders/{id}'; Status="SKIP"; Code=0; Message="No orderId" }
    Write-TestResult $result; $results += $result
}

# #7 Confirm
if ($orderId) {
    $result = Test-Endpoint -Name '#7 PUT /api/orders/{id}/confirm' -Method PUT `
        -Url "$BaseUrl/api/orders/$orderId/confirm" -Token $adminToken -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.newStatus -eq "Confirmed") { $true } else { "Not confirmed" } }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#7 PUT confirm'; Status="SKIP"; Code=0; Message="No orderId" }
    Write-TestResult $result; $results += $result
}

# #8 Ship
if ($orderId) {
    $result = Test-Endpoint -Name '#8 PUT /api/orders/{id}/ship' -Method PUT `
        -Url "$BaseUrl/api/orders/$orderId/ship" -Token $adminToken -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.newStatus -eq "Shipped" -and $j.data.trackingNumber) { $true } else { "Not shipped" } }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#8 PUT ship'; Status="SKIP"; Code=0; Message="No orderId" }
    Write-TestResult $result; $results += $result
}

# #9 Deliver
if ($orderId) {
    $result = Test-Endpoint -Name '#9 PUT /api/orders/{id}/deliver' -Method PUT `
        -Url "$BaseUrl/api/orders/$orderId/deliver" -Token $adminToken -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.newStatus -eq "Delivered") { $true } else { "Not delivered" } }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#9 PUT deliver'; Status="SKIP"; Code=0; Message="No orderId" }
    Write-TestResult $result; $results += $result
}

# #10 Cancel (create fresh order then cancel)
$cancelBody = '{"customerName":"Li Si","customerPhone":"13987654321","origin":{"street":"Jianguomen Outer St 1","city":"Beijing","province":"Beijing","postalCode":"100020","country":"CN"},"destination":{"street":"Tianfu Ave North 1700","city":"Chengdu","province":"Sichuan","postalCode":"610041","country":"CN"},"serviceLevel":"Standard","items":[{"description":"Books","quantity":3,"weight":{"value":1.5,"unit":"Kg"}}]}'
$cancelId = $null
try {
    $cancelResp = Invoke-Api -Method POST -Url "$BaseUrl/api/orders" -Body $cancelBody -Token $adminToken
    $cancelId = $cancelResp.data.orderId
} catch {}

if ($cancelId) {
    $result = Test-Endpoint -Name '#10 PUT /api/orders/{id}/cancel' -Method PUT `
        -Url "$BaseUrl/api/orders/$cancelId/cancel" -Token $adminToken `
        -Body '{"reason":"Customer requested cancellation"}' -ExpectedStatus @(200) `
        -Validate { param($j) if ($j.success -and $j.data.newStatus -eq "Cancelled") { $true } else { "Not cancelled" } }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#10 PUT cancel'; Status="SKIP"; Code=0; Message="Could not create order" }
    Write-TestResult $result; $results += $result
}

Write-Host ""

# ==============================================================================
# 3. ROUTING [3 endpoints]
# ==============================================================================
Write-Host '--- Routing [3 endpoints] ---' -ForegroundColor Yellow

# #11 Calculate Route
$routeBody = '{"origin":{"latitude":31.2304,"longitude":121.4737},"destination":{"latitude":39.9042,"longitude":116.4074},"packageWeight":{"value":2.5,"unit":"Kg"},"serviceLevel":"Express","strategy":"Fastest"}'
$result = Test-Endpoint -Name '#11 POST /api/routing/calculate [Fastest]' -Method POST `
    -Url "$BaseUrl/api/routing/calculate" -Body $routeBody -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.strategyUsed -eq "Fastest" -and $j.data.distanceKm -gt 0) { $true } else { "Invalid route" } }
Write-TestResult $result; $results += $result

# #11b Jin weight unit
$routeJinBody = '{"origin":{"latitude":31.2304,"longitude":121.4737},"destination":{"latitude":23.1291,"longitude":113.2644},"packageWeight":{"value":5.0,"unit":"Jin"},"serviceLevel":"Standard","strategy":"Cheapest"}'
$result = Test-Endpoint -Name '#11b POST /api/routing/calculate [Jin]' -Method POST `
    -Url "$BaseUrl/api/routing/calculate" -Body $routeJinBody -Token $adminToken -ExpectedStatus @(200)
Write-TestResult $result; $results += $result

# #12 Compare
$compareBody = '{"origin":{"latitude":31.2304,"longitude":121.4737},"destination":{"latitude":23.1291,"longitude":113.2644},"packageWeight":{"value":5.0,"unit":"Jin"},"serviceLevel":"Standard"}'
$result = Test-Endpoint -Name '#12 POST /api/routing/compare' -Method POST `
    -Url "$BaseUrl/api/routing/compare" -Body $compareBody -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.Count -eq 3) { $true } else { "Expected 3 strategies" } }
Write-TestResult $result; $results += $result

# #13 Strategies
$result = Test-Endpoint -Name '#13 GET /api/routing/strategies' -Method GET `
    -Url "$BaseUrl/api/routing/strategies" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data -contains "Fastest" -and $j.data -contains "Cheapest" -and $j.data -contains "Balanced") { $true } else { "Missing strategies" } }
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 4. CARRIERS [4 endpoints]
# ==============================================================================
Write-Host '--- Carriers [4 endpoints] ---' -ForegroundColor Yellow

# #14 List Carriers (Public)
$result = Test-Endpoint -Name '#14 GET /api/carriers [Public]' -Method GET `
    -Url "$BaseUrl/api/carriers" -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and ($j.data | Where-Object { $_.carrierCode -eq "SF" })) { $true } else { "Missing SF carrier" } }
Write-TestResult $result; $results += $result

# #15 Quotes
$quotesBody = '{"origin":{"street":"Lujiazui Ring Road 1000","city":"Shanghai","province":"Shanghai","postalCode":"200120","country":"CN"},"destination":{"street":"Jianguomen Outer St 1","city":"Beijing","province":"Beijing","postalCode":"100020","country":"CN"},"weight":{"value":2.5,"unit":"Kg"},"serviceLevel":"Express"}'
$result = Test-Endpoint -Name '#15 POST /api/carriers/quotes' -Method POST `
    -Url "$BaseUrl/api/carriers/quotes" -Body $quotesBody -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.quotes.Count -ge 1 -and $j.data.recommended) { $true } else { "No quotes" } }
Write-TestResult $result; $results += $result

# #16 Book SF
$bookBody = '{"origin":{"street":"Lujiazui Ring Road 1000","city":"Shanghai","province":"Shanghai","postalCode":"200120","country":"CN"},"destination":{"street":"Huacheng Avenue 18","city":"Guangzhou","province":"Guangdong","postalCode":"510623","country":"CN"},"weight":{"value":2.5,"unit":"Kg"},"sender":{"name":"Zhang San","phone":"13812345678","email":"zhangsan@example.com"},"recipient":{"name":"Li Si","phone":"13987654321","email":"lisi@example.com"},"serviceLevel":"Express"}'
$result = Test-Endpoint -Name '#16 POST /api/carriers/SF/book' -Method POST `
    -Url "$BaseUrl/api/carriers/SF/book" -Body $bookBody -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.carrierCode -eq "SF" -and $j.data.trackingNumber) { $true } else { "No booking" } }
Write-TestResult $result; $results += $result

# Extract tracking number
$trackingNumber = $null
try {
    $bookResp = Invoke-Api -Method POST -Url "$BaseUrl/api/carriers/SF/book" -Body $bookBody -Token $adminToken
    $trackingNumber = $bookResp.data.trackingNumber
} catch {}

# #17 Track
$trackNo = if ($trackingNumber) { $trackingNumber } else { "SF-TEST-001" }
$result = Test-Endpoint -Name '#17 GET /api/carriers/SF/track/{no}' -Method GET `
    -Url "$BaseUrl/api/carriers/SF/track/$trackNo" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.trackingNumber) { $true } else { "No tracking data" } }
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 5. TRACKING [2 endpoints]
# ==============================================================================
Write-Host '--- Tracking [2 endpoints] ---' -ForegroundColor Yellow

$testTrack = if ($trackingNumber) { $trackingNumber } else { "SF-TEST-001" }

# #18 Snapshot
$result = Test-Endpoint -Name '#18 GET /api/tracking/{no}/snapshot' -Method GET `
    -Url "$BaseUrl/api/tracking/$testTrack/snapshot" -Token $adminToken -ExpectedStatus @(200,404)
Write-TestResult $result; $results += $result

# #19 Subscribe
$result = Test-Endpoint -Name '#19 POST /api/tracking/{no}/subscribe' -Method POST `
    -Url "$BaseUrl/api/tracking/$testTrack/subscribe" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data.subscribed -eq $true) { $true } else { "Not subscribed" } }
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 6. AUDIT [2 endpoints]
# ==============================================================================
Write-Host '--- Audit [2 endpoints] ---' -ForegroundColor Yellow

$auditId = if ($orderId) { $orderId } else { "00000000-0000-0000-0000-000000000001" }

# #20 Audit by Entity
$result = Test-Endpoint -Name '#20 GET /api/audit/entity/Order/{id}' -Method GET `
    -Url "$BaseUrl/api/audit/entity/Order/$auditId" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data -is [array]) { $true } else { "Not an array" } }
Write-TestResult $result; $results += $result

# #21 Audit by Correlation
$result = Test-Endpoint -Name '#21 GET /api/audit/correlation/{id}' -Method GET `
    -Url "$BaseUrl/api/audit/correlation/seed-correlation-001" -Token $adminToken -ExpectedStatus @(200) `
    -Validate { param($j) if ($j.success -and $j.data -is [array]) { $true } else { "Not an array" } }
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 7. SECURITY CHECKS
# ==============================================================================
Write-Host '--- Security Checks ---' -ForegroundColor Yellow

# No token -> 401
$result = Test-Endpoint -Name 'SEC: No token -> 401' -Method GET `
    -Url "$BaseUrl/api/orders" -ExpectedStatus @(401)
Write-TestResult $result; $results += $result

# Viewer -> 403 on create order
if ($viewerToken) {
    $result = Test-Endpoint -Name 'SEC: Viewer -> 403 [create order]' -Method POST `
        -Url "$BaseUrl/api/orders" -Body $createOrderBody -Token $viewerToken -ExpectedStatus @(403)
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='SEC: Viewer 403'; Status="SKIP"; Code=0; Message="No viewer token" }
    Write-TestResult $result; $results += $result
}

Write-Host ""

# ==============================================================================
# SUMMARY
# ==============================================================================

$passCount = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$skipCount = ($results | Where-Object { $_.Status -eq "SKIP" }).Count
$totalCount = $results.Count

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Total:   $totalCount" -ForegroundColor White
Write-Host "  Passed:  $passCount" -ForegroundColor Green

if ($failCount -gt 0) {
    Write-Host "  Failed:  $failCount" -ForegroundColor Red
} else {
    Write-Host "  Failed:  0" -ForegroundColor Green
}

if ($skipCount -gt 0) {
    Write-Host "  Skipped: $skipCount" -ForegroundColor Yellow
}

Write-Host "============================================================" -ForegroundColor Cyan

if ($failCount -gt 0) {
    Write-Host ""
    Write-Host "  FAILED TESTS:" -ForegroundColor Red
    foreach ($f in ($results | Where-Object { $_.Status -eq "FAIL" })) {
        Write-Host "    - $($f.Name): $($f.Message)" -ForegroundColor Red
    }
}

Write-Host ""
if ($failCount -eq 0) {
    Write-Host "  ALL ENDPOINTS VERIFIED SUCCESSFULLY" -ForegroundColor Green
} else {
    Write-Host "  SOME ENDPOINTS FAILED - Review above" -ForegroundColor Red
}
Write-Host ""
