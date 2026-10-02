param([Parameter(Mandatory)][string]$Archive,[int]$Port=17896)
. "$PSScriptRoot/env.ps1"
$archivePath=(Resolve-Path -LiteralPath $Archive).Path
$runId=Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$evidence=Join-Path $ProjectRoot "artifacts/package-check-$runId"
$unpacked=Join-Path ([IO.Path]::GetTempPath()) "Hairball B test-$runId/朋友 试玩"
New-Item -ItemType Directory -Force -Path $evidence,$unpacked | Out-Null
Expand-Archive -LiteralPath $archivePath -DestinationPath $unpacked
$bundle=Join-Path $unpacked 'ProjectHairball-B'
$exe=Join-Path $bundle 'ProjectHairball-B.exe'
foreach($line in Get-Content "$bundle/SHA256SUMS.txt") {
    if($line -notmatch '^([A-F0-9]{64})  (.+)$'){throw 'Bad package manifest'}
    if((Get-FileHash -LiteralPath (Join-Path $bundle $Matches[2])).Hash -ne $Matches[1]){throw "Package integrity failure: $line"}
}
# Hide installed SDK/runtime from the child environment; also inspect the actual loaded CLR.
$savedEnvironment=@{}
foreach($key in @('DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_MULTILEVEL_LOOKUP','PATH')){$savedEnvironment[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
$env:DOTNET_ROOT=Join-Path $unpacked 'no-global-dotnet'
$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$env:PATH="$env:SystemRoot/System32;$env:SystemRoot"
$processes=@()
function Launch-Package([string]$label,[string[]]$arguments) {
    $log=Join-Path $evidence "$label.log"
    $proc=Start-Process -FilePath $exe -WorkingDirectory $unpacked -ArgumentList (@('--log-file',('"'+$log+'"'))+$arguments) -WindowStyle Hidden -PassThru -RedirectStandardError "$evidence/$label.err" -RedirectStandardOutput "$evidence/$label.stdout"
    return $proc
}
try {
    $menu=Launch-Package 'menu' @('--','--menu','--language','zh','--capture',('"'+$evidence+'/menu.png"'),'--quit-after-seconds','7')
    $processes+=$menu
    $loaded=@();$until=(Get-Date).AddSeconds(15)
    while(!$menu.HasExited -and (Get-Date) -lt $until) {
        $menu.Refresh();$loaded=@($menu.Modules | Where-Object ModuleName -eq 'coreclr.dll' | Select-Object -ExpandProperty FileName)
        if($loaded.Count){break};Start-Sleep -Milliseconds 200
    }
    if($loaded.Count -ne 1 -or !$loaded[0].StartsWith($bundle,[StringComparison]::OrdinalIgnoreCase)){throw "Bundled CLR was not observed: $loaded"}
    $loaded | Set-Content "$evidence/loaded-runtime.txt"
    if(!$menu.WaitForExit(20000) -or $menu.ExitCode -ne 0 -or !(Test-Path "$evidence/menu.png")){throw 'Packaged menu/capture failed'}
    $check=Launch-Package 'engine' @('--headless','--','--solo','--v06-check')
    $processes+=$check
    if(!$check.WaitForExit(30000) -or $check.ExitCode -ne 0){throw 'Packaged engine checks failed'}
    if((Get-Content "$evidence/engine.log" -Raw) -notmatch 'EXPERIMENT_VARIANT B' -or (Get-Content "$evidence/engine.log" -Raw) -notmatch 'V06_ENGINE_OK'){throw 'Packaged default variant/check mismatch'}
    $hostRun=Launch-Package 'host' @('--headless','--','--host','--full-smoke','--force-ai-customer','--expected','2','--port',"$Port",'--report',('"'+$evidence+'/host.json"'))
    $processes+=$hostRun
    Start-Sleep -Seconds 2
    $client=Launch-Package 'client' @('--headless','--','--join','127.0.0.1','--full-smoke','--force-ai-customer','--expected','2','--port',"$Port",'--report',('"'+$evidence+'/client.json"'))
    $processes+=$client
    $until=(Get-Date).AddSeconds(240)
    while(!$hostRun.HasExited -or !$client.HasExited){if((Get-Date) -gt $until){throw 'Packaged multiplayer timed out'};Start-Sleep -Milliseconds 500}
    foreach($label in @('host','client')) {
        $report=Get-Content "$evidence/$label.json" -Raw | ConvertFrom-Json
        if(!$report.success -or $report.variant -ne 'B' -or !$report.liveResolved){throw "Packaged $label failed"}
        foreach($fact in @('leave:ToMirror','leave:Mirror','leave:ToDoor','attention:Notice','attention:Commit','attention:React','brace')) {
            if($report.experimentFacts -notcontains $fact){throw "$label missed $fact"}
        }
    }
    $hostText=Get-Content "$evidence/host.log" -Raw
    $clientText=Get-Content "$evidence/client.log" -Raw
    $hash=[regex]::Match($clientText,'CLIENT_FINAL[^\r\n]*hash=([A-F0-9]+)').Groups[1].Value
    if(!$hash -or $hostText -notmatch "HOST_FINAL_HASH $hash"){throw 'Packaged peers disagree on final state'}
    foreach($p in $processes){if($p.ExitCode -ne 0){throw "Process $($p.Id) failed: $($p.ExitCode)"}}
    foreach($f in Get-ChildItem $evidence -File | Where-Object Extension -in '.log','.err','.stdout') {
        if((Get-Content -LiteralPath $f.FullName -Raw) -match 'ERROR:|Unhandled exception|SMOKE_FAIL'){throw "Engine error: $($f.Name)"}
    }
    @{archive=$archivePath;sha256=(Get-FileHash -LiteralPath $archivePath).Hash;unpacked=$bundle;loadedRuntime=$loaded[0];variant='B';fullDuration=$true;peers=2;finalHash=$hash;passed=$true} | ConvertTo-Json | Set-Content "$evidence/verification.json"
    Write-Host "PACKAGE_VERIFY_OK $evidence"
} finally {
    foreach($p in $processes){if(!$p.HasExited){Stop-Process -Id $p.Id}}
    foreach($key in $savedEnvironment.Keys){[Environment]::SetEnvironmentVariable($key,$savedEnvironment[$key],'Process')}
}
