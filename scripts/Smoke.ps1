param([string]$BaseUrl = 'https://localhost:7028')
$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/') + '/api/v1'
$passed = 0

function Request([string]$Method, [string]$Path, [int]$Expected, $Body = $null, [string]$Token = '', [string]$Key = '') {
    $argsList = @('-sS', '-X', $Method, "$base$Path", '-w', "`n%{http_code}")
    if ($Token) { $argsList += @('-H', "Authorization: Bearer $Token") }
    if ($Key) { $argsList += @('-H', "Idempotency-Key: $Key") }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Compress -Depth 8
        $argsList += @('-H', 'Content-Type: application/json', '--data-binary', '@-')
        $raw = ($json | & curl.exe @argsList) -join "`n"
    } else { $raw = (& curl.exe @argsList) -join "`n" }
    if ($LASTEXITCODE -ne 0) { throw "curl failed: $Method $Path" }
    $parts = $raw -split "`n"
    $code = [int]$parts[-1]
    if ($code -ne $Expected) { throw "$Method $Path : expected $Expected, received $code. $raw" }
    $script:passed++
    Write-Host "$Method $Path | HTTP $code"
    $text = ($parts | Select-Object -SkipLast 1) -join "`n"
    if ($text.Trim()) { return ($text | ConvertFrom-Json) }
}
function Check([bool]$Condition, [string]$Description) {
    if (-not $Condition) { throw $Description }
    Write-Host "OK | $Description"
}

$null = Request GET '/health/ready' 200
$email = "smoke-$([guid]::NewGuid().ToString('N'))@example.com"
$auth = @{ email=$email; password='Smoke-password-2026' }
$null = Request POST '/auth/register' 201 $auth
$null = Request POST '/auth/register' 409 $auth
$login = Request POST '/auth/login' 200 $auth
$token = $login.accessToken
$input = @{name='Music';category='Media';price='100.00';currency='RUB';frequency='monthly';nextPaymentDate='2026-12-01';status='active'}
$sub = Request POST '/subscriptions' 201 $input $token
$path = '/subscriptions/' + $sub.id
$null = Request GET $path 200 $null $token
$list = Request GET '/subscriptions?status=active&page=1&pageSize=20' 200 $null $token
Check ($list.totalCount -eq 1) 'Subscription appears in filtered list'
$input.price='200.00'
$updated = Request PUT $path 200 $input $token
Check ($updated.price -eq '200.00') 'PUT replaces editable fields'
$today=[DateTime]::UtcNow.Date
$from=$today.ToString('yyyy-MM-dd'); $to=$today.AddDays(1).ToString('yyyy-MM-dd')
$key=[guid]::NewGuid().ToString('N')
$payment=@{amount='100.00';currency='RUB';paidOn=$from}
$one=Request POST "$path/payments" 201 $payment $token $key
$two=Request POST "$path/payments" 201 $payment $token $key
Check ($one.id -eq $two.id) 'Replay returns the same payment ID'
$payment.amount='101.00'
$null=Request POST "$path/payments" 409 $payment $token $key
$history=Request GET "/payments?from=$from&to=$to&subscriptionId=$($sub.id)" 200 $null $token
Check ($history.totalCount -eq 1) 'Payment is stored exactly once'
$totals=Request GET "/analytics/expenses?from=$from&to=$to" 200 $null $token
Check ($totals.byCurrency[0].amount -eq '100.00') 'Actual expenses are 100.00 RUB'
$auth.email="other-$email"
$null=Request POST '/auth/register' 201 $auth
$other=Request POST '/auth/login' 200 $auth
$null=Request GET $path 404 $null $other.accessToken
$null=Request POST "$path/payments" 404 $payment $other.accessToken ([guid]::NewGuid().ToString('N'))
$input.price='0.00'
$null=Request POST '/subscriptions' 400 $input $token
$null=Request GET '/subscriptions?pageSize=101' 400 $null $token
$null=Request DELETE $path 204 $null $token
$null=Request DELETE $path 204 $null $token
$archived=Request GET $path 200 $null $token
Check ($archived.status -eq 'archived') 'DELETE archives the subscription'
$payment.amount='50.00'
$null=Request POST "$path/payments" 201 $payment $token ([guid]::NewGuid().ToString('N'))
$null=Request POST '/auth/logout' 204 $null $token
$null=Request GET '/subscriptions' 401 $null $token
Write-Host "All checks passed. HTTP requests: $passed"
