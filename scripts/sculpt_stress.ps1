param([int]$Duration=600,[int]$Port=18794,[switch]$RenderedHost)
. "$PSScriptRoot/env.ps1"
$outDir=Join-Path $ProjectRoot ('artifacts/sculpt-stress-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
Write-Host "Stress evidence: $outDir"
$processes=@()
try {
    for ($i=0;$i -lt 4;$i++) {
        if ($i -eq 3) { Start-Sleep -Seconds 12 }
        $launch=@('--path',('"'+$ProjectRoot+'"'),'--','--sculpt-stress','--expected','3','--duration',$Duration,'--port',$Port,'--report',('"'+$outDir+"/peer-$i.json"+'"'))
        if (-not $RenderedHost -or $i -ne 0) { $launch=@('--headless')+$launch }
        if ($i -eq 0) { $launch+='--host' } else { $launch+=@('--join','127.0.0.1') }
        $processes+=Start-Process -FilePath (Find-Godot) -ArgumentList $launch -WindowStyle Hidden -PassThru -RedirectStandardOutput "$outDir/peer-$i.log" -RedirectStandardError "$outDir/peer-$i.err"
        if ($i -eq 0) { Start-Sleep -Seconds 2 }
    }
    $deadline=(Get-Date).AddSeconds($Duration*3+120)
    while (@($processes|Where-Object {-not $_.HasExited}).Count -gt 0) {
        if ((Get-Date) -gt $deadline) { throw 'Sculpt stress timed out' }
        Start-Sleep -Seconds 1
    }
    for ($i=0;$i -lt 4;$i++) {
        if ($processes[$i].ExitCode -ne 0) { throw "Peer $i failed" }
        $report=Get-Content "$outDir/peer-$i.json" -Raw | ConvertFrom-Json
        if (-not $report.success) { throw "Peer $i failed: $($report.reason)" }
        if ((Get-Content "$outDir/peer-$i.err" -Raw) -match 'ERROR:|Exception') { throw "Peer $i engine error" }
    }
    $hostLog=Get-Content "$outDir/peer-0.log" -Raw
    $hash=[regex]::Match($hostLog,'SCULPT_STRESS_FINAL_HASH ([A-F0-9]+)').Groups[1].Value
    for ($i=1;$i -lt 4;$i++) { if ((Get-Content "$outDir/peer-$i.log" -Raw) -notmatch "SCULPT_STRESS_CLIENT_HASH $hash") { throw "Peer $i final debris mismatch" } }
    $late=Get-Content "$outDir/peer-3.log" -Raw
    $joinedMass=[double]([regex]::Match($late,'JOIN_SNAPSHOT mass=([0-9.]+)').Groups[1].Value)
    if ($joinedMass -le 0) { throw 'Late join did not receive pre-existing debris' }
    & "$PSScriptRoot/summarize_sculpt_stress.ps1" -Directory $outDir | Out-Null
    Get-Content "$outDir/peer-0.log" | Select-Object -Last 8
    Write-Host "SCULPT_STRESS_OK duration=$Duration peers=4 lateJoinMass=$joinedMass hash=$hash"
} finally {
    foreach ($proc in $processes) { if (-not $proc.HasExited) { & taskkill /PID $proc.Id /T /F | Out-Null } }
}
