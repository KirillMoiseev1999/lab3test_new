$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$results = Join-Path $root ('TestResults/run-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $results -Force | Out-Null
$log = Join-Path $results 'test.log'
& dotnet test (Join-Path $root 'Subscriptions.sln') --collect:'XPlat Code Coverage' --results-directory $results --logger trx -m:1 *> $log
if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $log; throw 'Tests failed. See test.log.' }
$total=0; $passed=0; $failed=0
Get-ChildItem -LiteralPath $results -Filter '*.trx' | ForEach-Object {
    [xml]$trx=Get-Content -LiteralPath $_.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    $total += [int]$counts.total; $passed += [int]$counts.passed; $failed += [int]$counts.failed
}
Write-Host "NUnit | Total $total | Passed $passed | Failed $failed"
$business=$null
foreach($file in (Get-ChildItem -LiteralPath $results -Recurse -Filter coverage.cobertura.xml)) {
    [xml]$coverage=Get-Content -LiteralPath $file.FullName
    $packages=@($coverage.coverage.packages.package)
    # Select the isolated Business.Tests run, not the HTTP run or a filtered subset.
    if($packages.Count -eq 1 -and $packages[0].name -eq 'Subscriptions.Business') {
        $business=$packages[0]; break
    }
}
if($null -eq $business) { throw 'Business assembly coverage was not found.' }
$culture=[Globalization.CultureInfo]::InvariantCulture
$lines=[double]::Parse($business.GetAttribute('line-rate'),$culture)*100
$branches=[double]::Parse($business.GetAttribute('branch-rate'),$culture)*100
Write-Host ('Business | Lines {0:F2}% | Branches {1:F2}%' -f $lines,$branches)
Write-Host 'Statements: not measured by this report.'
Write-Host "Full reports: $results"
if($lines -lt 90 -or $branches -lt 80) { throw 'Line/branch coverage target not reached.' }
