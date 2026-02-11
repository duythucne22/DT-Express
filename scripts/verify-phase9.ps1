# ==============================================================================
# DT-Express TMS - Phase 9 Verification Script
# ==============================================================================
# Tests all 6 new Phase 9 endpoints (Dashboard + Advanced Orders).
# Usage: .\scripts\verify-phase9.ps1 [-BaseUrl "http://localhost:5198"]
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
        "X-Correlation-ID" = "p9-verify-$(Get-Random)"
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

        $response   = Invoke-WebRequest @params
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
Write-Host "  DT-Express TMS - Phase 9 Verification (6 endpoints)" -ForegroundColor Cyan
Write-Host "  Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "  Time:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ==============================================================================
# 0. AUTHENTICATE
# ==============================================================================
Write-Host '--- Authenticating ---' -ForegroundColor Yellow

$adminLogin = '{"username":"admin","password":"admin123"}'
$adminToken = ""
$dispatcherToken = ""
try {
    $lr = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $adminLogin
    $adminToken = $lr.data.accessToken
    Write-Host "  [OK] Admin token acquired" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Cannot login as admin - aborting" -ForegroundColor Red
    exit 1
}

$dispatcherLogin = '{"username":"dispatcher","password":"passwd123"}'
try {
    $lr2 = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $dispatcherLogin
    $dispatcherToken = $lr2.data.accessToken
    Write-Host "  [OK] Dispatcher token acquired" -ForegroundColor Green
} catch {
    Write-Host "  [WARN] Cannot login as dispatcher - some tests may fail" -ForegroundColor Yellow
}

Write-Host ""

# ==============================================================================
# 1. DASHBOARD ENDPOINTS [3 endpoints]
# ==============================================================================
Write-Host '--- Dashboard [3 endpoints] ---' -ForegroundColor Yellow

# #1 GET /api/dashboard/stats
$result = Test-Endpoint -Name '#1 GET /api/dashboard/stats' -Method GET `
    -Url "$BaseUrl/api/dashboard/stats" -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        $d = $j.data
        if ($j.success -and $null -ne $d.totalOrders -and $null -ne $d.monthRevenue -and $null -ne $d.statusBreakdown) {
            $true
        } else {
            "Missing totalOrders/monthRevenue/statusBreakdown"
        }
    }
Write-TestResult $result; $results += $result

# #2 GET /api/dashboard/carrier-performance
$result = Test-Endpoint -Name '#2 GET /api/dashboard/carrier-performance' -Method GET `
    -Url "$BaseUrl/api/dashboard/carrier-performance" -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        if ($j.success -and $null -ne $j.data) { $true } else { "Missing data" }
    }
Write-TestResult $result; $results += $result

# #3 GET /api/dashboard/top-customers
$result = Test-Endpoint -Name '#3 GET /api/dashboard/top-customers' -Method GET `
    -Url "$BaseUrl/api/dashboard/top-customers?limit=3" -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        if ($j.success -and $null -ne $j.data) { $true } else { "Missing data" }
    }
Write-TestResult $result; $results += $result

# Dashboard - Auth test (dispatcher can access carrier-performance)
$result = Test-Endpoint -Name '#3b GET /api/dashboard/top-customers [Dispatcher=403]' -Method GET `
    -Url "$BaseUrl/api/dashboard/top-customers?limit=3" -Token $dispatcherToken -ExpectedStatus @(403) `
    -Validate { param($j) $true }
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 2. ADVANCED ORDER ENDPOINTS [3 endpoints]
# ==============================================================================
Write-Host '--- Advanced Orders [3 endpoints] ---' -ForegroundColor Yellow

# First, create a regular order to use for update-destination and split-shipment tests
Write-Host "  Setting up test orders..." -ForegroundColor DarkGray

$createOrderBody = @'
{
  "customerName": "Phase9 Test Customer",
  "customerPhone": "13800009999",
  "customerEmail": "phase9test@example.com",
  "origin": {
    "street": "100 Origin St",
    "city": "Toronto",
    "province": "ON",
    "postalCode": "M5V 1A1",
    "country": "CA"
  },
  "destination": {
    "street": "200 Dest Ave",
    "city": "Vancouver",
    "province": "BC",
    "postalCode": "V6B 2K8",
    "country": "CA"
  },
  "serviceLevel": "Standard",
  "items": [
    { "description": "Item A", "weight": { "value": 2.0, "unit": "kg" }, "quantity": 1 },
    { "description": "Item B", "weight": { "value": 3.0, "unit": "kg" }, "quantity": 2 },
    { "description": "Item C", "weight": { "value": 1.5, "unit": "kg" }, "quantity": 1 }
  ]
}
'@

$testOrderId1 = $null
$testOrderId2 = $null

try {
    $cr1 = Invoke-Api -Method POST -Url "$BaseUrl/api/orders" -Body $createOrderBody -Token $adminToken
    $testOrderId1 = $cr1.data.orderId
    Write-Host "  [OK] Test order 1: $testOrderId1" -ForegroundColor DarkGray
} catch {
    Write-Host "  [WARN] Could not create test order 1: $($_.Exception.Message)" -ForegroundColor Yellow
}

try {
    $cr2 = Invoke-Api -Method POST -Url "$BaseUrl/api/orders" -Body $createOrderBody -Token $adminToken
    $testOrderId2 = $cr2.data.orderId
    Write-Host "  [OK] Test order 2: $testOrderId2" -ForegroundColor DarkGray
} catch {
    Write-Host "  [WARN] Could not create test order 2: $($_.Exception.Message)" -ForegroundColor Yellow
}

# #4 POST /api/orders/bulk-create
$bulkBody = @'
{
  "orders": [
    {
      "customerName": "Bulk Customer A",
      "customerPhone": "13900001001",
      "customerEmail": "bulk-a@example.com",
      "origin": {
        "street": "10 Warehouse Rd",
        "city": "Toronto",
        "province": "ON",
        "postalCode": "M5V 1A1",
        "country": "CA"
      },
      "destination": {
        "street": "20 Delivery St",
        "city": "Ottawa",
        "province": "ON",
        "postalCode": "K1A 0B1",
        "country": "CA"
      },
      "serviceLevel": "Express",
      "items": [
        { "description": "Widget X", "weight": { "value": 1.0, "unit": "kg" }, "quantity": 5 }
      ]
    },
    {
      "customerName": "Bulk Customer B",
      "customerPhone": "13900001002",
      "customerEmail": "bulk-b@example.com",
      "origin": {
        "street": "30 Factory Lane",
        "city": "Montreal",
        "province": "QC",
        "postalCode": "H2X 1Y4",
        "country": "CA"
      },
      "destination": {
        "street": "40 Home Ave",
        "city": "Calgary",
        "province": "AB",
        "postalCode": "T2P 0G5",
        "country": "CA"
      },
      "serviceLevel": "Standard",
      "items": [
        { "description": "Gadget Y", "weight": { "value": 2.5, "unit": "kg" }, "quantity": 3 },
        { "description": "Gadget Z", "weight": { "value": 0.8, "unit": "kg" }, "quantity": 10 }
      ]
    }
  ]
}
'@

$result = Test-Endpoint -Name '#4 POST /api/orders/bulk-create' -Method POST `
    -Url "$BaseUrl/api/orders/bulk-create" -Body $bulkBody -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        $d = $j.data
        if ($j.success -and $d.successCount -eq 2 -and $d.failureCount -eq 0 -and $d.results.Count -eq 2) {
            $first = $d.results[0]
            if ($first.success -and $first.orderId -and $first.orderNumber) { $true }
            else { "Result item missing orderId/orderNumber" }
        } else {
            "Expected 2 success, got: success=$($d.successCount) failure=$($d.failureCount)"
        }
    }
Write-TestResult $result; $results += $result

# #5 PUT /api/orders/{id}/update-destination
if ($testOrderId1) {
    $updateDestBody = @'
{
  "destination": {
    "street": "999 New Delivery Blvd",
    "city": "Edmonton",
    "province": "AB",
    "postalCode": "T5J 0N3",
    "country": "CA"
  }
}
'@

    $result = Test-Endpoint -Name '#5 PUT /api/orders/{id}/update-destination' -Method PUT `
        -Url "$BaseUrl/api/orders/$testOrderId1/update-destination" -Body $updateDestBody -Token $adminToken `
        -ExpectedStatus @(200) `
        -Validate {
            param($j)
            $d = $j.data
            if ($j.success -and $d.orderId -and $d.newDestination -and $d.status) {
                if ($d.newDestination -like "*Edmonton*") { $true }
                else { "Destination not updated to Edmonton, got: $($d.newDestination)" }
            } else {
                "Missing orderId/newDestination/status"
            }
        }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#5 PUT /api/orders/{id}/update-destination'; Status="FAIL"; Code=0; Message="No test order available" }
    Write-TestResult $result; $results += $result
}

# #6 POST /api/orders/{id}/split-shipment
if ($testOrderId2) {
    $splitBody = @'
{
  "groups": [[0], [1, 2]]
}
'@

    $result = Test-Endpoint -Name '#6 POST /api/orders/{id}/split-shipment' -Method POST `
        -Url "$BaseUrl/api/orders/$testOrderId2/split-shipment" -Body $splitBody -Token $adminToken `
        -ExpectedStatus @(200) `
        -Validate {
            param($j)
            $d = $j.data
            if ($j.success -and $d.originalOrderId -and $d.newOrders -and $d.newOrders.Count -eq 2) {
                $o1 = $d.newOrders[0]
                $o2 = $d.newOrders[1]
                if ($o1.orderId -and $o1.orderNumber -and $o2.orderId -and $o2.orderNumber) {
                    if ($o1.itemCount -eq 1 -and $o2.itemCount -eq 2) { $true }
                    else { "Item counts wrong: $($o1.itemCount), $($o2.itemCount)" }
                } else { "New orders missing orderId/orderNumber" }
            } else {
                "Missing originalOrderId or expected 2 new orders"
            }
        }
    Write-TestResult $result; $results += $result
} else {
    $result = @{ Name='#6 POST /api/orders/{id}/split-shipment'; Status="FAIL"; Code=0; Message="No test order available" }
    Write-TestResult $result; $results += $result
}

# Negative: split on non-existent order
$fakeId = [Guid]::NewGuid().ToString()
$splitBody2 = '{"groups":[[0],[1]]}'
$result = Test-Endpoint -Name '#6b POST /api/orders/{id}/split [404 non-existent]' -Method POST `
    -Url "$BaseUrl/api/orders/$fakeId/split-shipment" -Body $splitBody2 -Token $adminToken `
    -ExpectedStatus @(404)
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# SUMMARY
# ==============================================================================
$pass = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$fail = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $results.Count

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Phase 9 Results: $pass/$total PASSED" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Yellow" })
if ($fail -gt 0) {
    Write-Host "  Failed tests:" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "    - $($_.Name): $($_.Message)" -ForegroundColor Red
    }
}
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
