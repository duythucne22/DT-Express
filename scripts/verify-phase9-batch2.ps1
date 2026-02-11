# ==============================================================================
# DT-Express TMS - Phase 9 Batch 2 Verification Script
# ==============================================================================
# Tests 4 new endpoints: Webhook, Monthly Report (JSON+CSV), Revenue Report.
# Also tests SignalR hub negotiation.
# Usage: .\scripts\verify-phase9-batch2.ps1 [-BaseUrl "http://localhost:5198"]
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
        [hashtable]$ExtraHeaders = @{},
        [scriptblock]$Validate = $null,
        [switch]$RawResponse
    )

    $headers = @{
        "Content-Type"     = "application/json"
        "X-Correlation-ID" = "p9b2-verify-$(Get-Random)"
    }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    foreach ($k in $ExtraHeaders.Keys) { $headers[$k] = $ExtraHeaders[$k] }

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

        if ($RawResponse) {
            if ($ExpectedStatus -contains $statusCode) {
                if ($Validate) {
                    $vr = & $Validate $response
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

        $json = $response.Content | ConvertFrom-Json

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

function Get-HmacSha256 {
    param([string]$Message, [string]$Secret)
    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $msgBytes = [System.Text.Encoding]::UTF8.GetBytes($Message)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hashBytes = $hmac.ComputeHash($msgBytes)
    return [BitConverter]::ToString($hashBytes).Replace("-", "").ToLower()
}

# -- Banner --------------------------------------------------------------------

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  DT-Express TMS - Phase 9 Batch 2 Verification" -ForegroundColor Cyan
Write-Host "  Endpoints: Webhook, Reports (Monthly+Revenue), SignalR Hub" -ForegroundColor Cyan
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
$viewerToken = ""
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
    Write-Host "  [WARN] Cannot login as dispatcher" -ForegroundColor Yellow
}

$viewerLogin = '{"username":"viewer","password":"passwd123"}'
try {
    $lr3 = Invoke-Api -Method POST -Url "$BaseUrl/api/auth/login" -Body $viewerLogin
    $viewerToken = $lr3.data.accessToken
    Write-Host "  [OK] Viewer token acquired" -ForegroundColor Green
} catch {
    Write-Host "  [WARN] Cannot login as viewer" -ForegroundColor Yellow
}

Write-Host ""

# ==============================================================================
# 1. WEBHOOK ENDPOINT
# ==============================================================================
Write-Host '--- Webhook [POST /api/webhooks/carrier/{code}] ---' -ForegroundColor Yellow

$webhookSecret = "dt-express-webhook-secret-2026"

# #1 Valid webhook with HMAC signature
$webhookBody = @'
{"trackingNumber":"SF0000000001","status":"InTransit","description":"Package arrived at sorting center","latitude":31.2304,"longitude":121.4737,"occurredAt":"2026-01-15T10:30:00Z"}
'@

$signature = Get-HmacSha256 -Message $webhookBody -Secret $webhookSecret

$result = Test-Endpoint -Name '#1 POST /api/webhooks/carrier/SF [valid HMAC]' -Method POST `
    -Url "$BaseUrl/api/webhooks/carrier/SF" `
    -Body $webhookBody `
    -ExtraHeaders @{ "X-Webhook-Signature" = "sha256=$signature" } `
    -ExpectedStatus @(200) `
    -Validate {
        param($j)
        $d = $j.data
        if ($j.success -and $d.accepted -eq $true -and $d.trackingNumber -eq "SF0000000001" -and $d.status -eq "InTransit" -and $d.carrierCode -eq "SF") {
            $true
        } else {
            "Expected accepted=true, trackingNumber=SF0000000001, status=InTransit. Got: $($j | ConvertTo-Json -Compress)"
        }
    }
Write-TestResult $result; $results += $result

# #2 Webhook without signature → 401
$result = Test-Endpoint -Name '#2 POST /api/webhooks/carrier/SF [no signature=401]' -Method POST `
    -Url "$BaseUrl/api/webhooks/carrier/SF" `
    -Body $webhookBody `
    -ExpectedStatus @(401)
Write-TestResult $result; $results += $result

# #3 Webhook with wrong signature → 401
$result = Test-Endpoint -Name '#3 POST /api/webhooks/carrier/SF [wrong sig=401]' -Method POST `
    -Url "$BaseUrl/api/webhooks/carrier/SF" `
    -Body $webhookBody `
    -ExtraHeaders @{ "X-Webhook-Signature" = "sha256=0000000000000000000000000000000000000000000000000000000000000000" } `
    -ExpectedStatus @(401)
Write-TestResult $result; $results += $result

# #4 Webhook with invalid status → 400
$badStatusBody = '{"trackingNumber":"SF0000000001","status":"InvalidStatus","description":"test"}'
$badSig = Get-HmacSha256 -Message $badStatusBody -Secret $webhookSecret

$result = Test-Endpoint -Name '#4 POST /api/webhooks/carrier/JD [bad status=400]' -Method POST `
    -Url "$BaseUrl/api/webhooks/carrier/JD" `
    -Body $badStatusBody `
    -ExtraHeaders @{ "X-Webhook-Signature" = "sha256=$badSig" } `
    -ExpectedStatus @(400)
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 2. MONTHLY SHIPMENTS REPORT
# ==============================================================================
Write-Host '--- Monthly Report [GET /api/reports/shipments/monthly] ---' -ForegroundColor Yellow

# Get current month for testing
$currentMonth = Get-Date -Format "yyyy-MM"

# #5 Monthly report (JSON format)
$result = Test-Endpoint -Name '#5 GET /reports/shipments/monthly [JSON]' -Method GET `
    -Url "$BaseUrl/api/reports/shipments/monthly?month=$currentMonth&format=json" `
    -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        $d = $j.data
        if ($j.success -and $null -ne $d.year -and $null -ne $d.month -and $null -ne $d.totalShipments -and $null -ne $d.totalRevenue -and $null -ne $d.currency) {
            $true
        } else {
            "Missing year/month/totalShipments/totalRevenue/currency in response"
        }
    }
Write-TestResult $result; $results += $result

# #6 Monthly report (CSV format) — check Content-Type
$result = Test-Endpoint -Name '#6 GET /reports/shipments/monthly [CSV]' -Method GET `
    -Url "$BaseUrl/api/reports/shipments/monthly?month=$currentMonth&format=csv" `
    -Token $adminToken -ExpectedStatus @(200) `
    -RawResponse `
    -Validate {
        param($resp)
        $ct = $resp.Headers["Content-Type"]
        # Content-Type may be an array
        $ctString = if ($ct -is [array]) { $ct[0] } else { $ct.ToString() }
        if ($ctString -like "*text/csv*") {
            $content = $resp.Content
            if ($content -like "*OrderId*OrderNumber*CustomerName*") {
                $true
            } else {
                "CSV content missing expected headers. Got: $($content.Substring(0, [Math]::Min(200, $content.Length)))"
            }
        } else {
            "Expected Content-Type text/csv, got: $ctString"
        }
    }
Write-TestResult $result; $results += $result

# #7 Monthly report - bad month format → 400
$result = Test-Endpoint -Name '#7 GET /reports/shipments/monthly [bad format=400]' -Method GET `
    -Url "$BaseUrl/api/reports/shipments/monthly?month=not-a-month&format=json" `
    -Token $adminToken -ExpectedStatus @(400)
Write-TestResult $result; $results += $result

# #8 Monthly report - dispatcher can access
$result = Test-Endpoint -Name '#8 GET /reports/shipments/monthly [Dispatcher OK]' -Method GET `
    -Url "$BaseUrl/api/reports/shipments/monthly?month=$currentMonth&format=json" `
    -Token $dispatcherToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        if ($j.success) { $true } else { "Dispatcher should have access" }
    }
Write-TestResult $result; $results += $result

# #9 Monthly report - viewer denied (Roles: Admin,Dispatcher)
$result = Test-Endpoint -Name '#9 GET /reports/shipments/monthly [Viewer=403]' -Method GET `
    -Url "$BaseUrl/api/reports/shipments/monthly?month=$currentMonth&format=json" `
    -Token $viewerToken -ExpectedStatus @(403)
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 3. REVENUE BY CARRIER REPORT
# ==============================================================================
Write-Host '--- Revenue Report [GET /api/reports/revenue/by-carrier] ---' -ForegroundColor Yellow

# #10 Revenue report (Admin)
$result = Test-Endpoint -Name '#10 GET /reports/revenue/by-carrier [Admin]' -Method GET `
    -Url "$BaseUrl/api/reports/revenue/by-carrier?from=2025-01-01&to=2027-12-31" `
    -Token $adminToken -ExpectedStatus @(200) `
    -Validate {
        param($j)
        $d = $j.data
        if ($j.success -and $null -ne $d.fromDate -and $null -ne $d.toDate -and $null -ne $d.grandTotal -and $null -ne $d.currency -and $null -ne $d.carriers) {
            $true
        } else {
            "Missing fromDate/toDate/grandTotal/currency/carriers"
        }
    }
Write-TestResult $result; $results += $result

# #11 Revenue report - bad date → 400
$result = Test-Endpoint -Name '#11 GET /reports/revenue/by-carrier [bad date=400]' -Method GET `
    -Url "$BaseUrl/api/reports/revenue/by-carrier?from=not-a-date&to=2027-12-31" `
    -Token $adminToken -ExpectedStatus @(400)
Write-TestResult $result; $results += $result

# #12 Revenue report - dispatcher denied (Roles: Admin only)
$result = Test-Endpoint -Name '#12 GET /reports/revenue/by-carrier [Dispatcher=403]' -Method GET `
    -Url "$BaseUrl/api/reports/revenue/by-carrier?from=2025-01-01&to=2027-12-31" `
    -Token $dispatcherToken -ExpectedStatus @(403)
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# 4. SIGNALR HUB NEGOTIATION
# ==============================================================================
Write-Host '--- SignalR Hub [/hubs/tracking] ---' -ForegroundColor Yellow

# #13 SignalR negotiate endpoint (requires JWT via query string)
try {
    $negotiateUrl = "$BaseUrl/hubs/tracking/negotiate?negotiateVersion=1&access_token=$adminToken"
    $negotiateResp = Invoke-WebRequest -Uri $negotiateUrl -Method POST -ErrorAction Stop
    $sc = $negotiateResp.StatusCode
    if ($sc -eq 200) {
        $negotiateJson = $negotiateResp.Content | ConvertFrom-Json
        if ($negotiateJson.connectionId) {
            $result = @{ Name='#13 POST /hubs/tracking/negotiate [SignalR]'; Status="PASS"; Code=200; Message="OK - connectionId: $($negotiateJson.connectionId.Substring(0, 8))..." }
        } else {
            $result = @{ Name='#13 POST /hubs/tracking/negotiate [SignalR]'; Status="PASS"; Code=200; Message="OK - negotiate returned data" }
        }
    } else {
        $result = @{ Name='#13 POST /hubs/tracking/negotiate [SignalR]'; Status="FAIL"; Code=$sc; Message="Unexpected status" }
    }
} catch {
    $er = $_.Exception.Response
    $sc = if ($er) { [int]$er.StatusCode } else { 0 }
    $result = @{ Name='#13 POST /hubs/tracking/negotiate [SignalR]'; Status="FAIL"; Code=$sc; Message=$_.Exception.Message }
}
Write-TestResult $result; $results += $result

# #14 SignalR negotiate without token → 401
try {
    $negotiateResp = Invoke-WebRequest -Uri "$BaseUrl/hubs/tracking/negotiate?negotiateVersion=1" -Method POST -ErrorAction Stop
    $result = @{ Name='#14 POST /hubs/tracking/negotiate [no auth=401]'; Status="FAIL"; Code=$negotiateResp.StatusCode; Message="Expected 401 but got $($negotiateResp.StatusCode)" }
} catch {
    $er = $_.Exception.Response
    $sc = if ($er) { [int]$er.StatusCode } else { 0 }
    if ($sc -eq 401) {
        $result = @{ Name='#14 POST /hubs/tracking/negotiate [no auth=401]'; Status="PASS"; Code=401; Message="OK (expected 401)" }
    } else {
        $result = @{ Name='#14 POST /hubs/tracking/negotiate [no auth=401]'; Status="FAIL"; Code=$sc; Message=$_.Exception.Message }
    }
}
Write-TestResult $result; $results += $result

Write-Host ""

# ==============================================================================
# SUMMARY
# ==============================================================================
$pass = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$fail = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $results.Count

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Phase 9 Batch 2 Results: $pass/$total PASSED" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Yellow" })
if ($fail -gt 0) {
    Write-Host "  Failed tests:" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "    - $($_.Name): $($_.Message)" -ForegroundColor Red
    }
}
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
