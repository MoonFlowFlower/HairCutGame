param([switch]$Network,[switch]$FullDuration,[switch]$Stress)
. "$PSScriptRoot/env.ps1"
$outDir=Join-Path $ProjectRoot ('artifacts/verify-v04-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
foreach ($check in @('build','test','integration','shared_check','access_check','sculpt_check','localization','laundry_check','ego_check','walkthrough','host_loss')) {
    & "$PSScriptRoot/$check.ps1" *> (Join-Path $outDir "$check.log")
    if ($LASTEXITCODE -ne 0) { throw "$check failed; see $outDir" }
    Write-Host "V04_VERIFY_PASS $check"
}
if ($Network) {
    & "$PSScriptRoot/smoke.ps1" -Players 1 -FullDuration:$FullDuration -Port 17901
    & "$PSScriptRoot/smoke.ps1" -Players 2 -Ego -FullDuration:$FullDuration -Port 17902
    & "$PSScriptRoot/smoke.ps1" -Players 4 -Ego -Port 17903
    & "$PSScriptRoot/smoke.ps1" -Players 4 -DisconnectClient -Port 17904
}
if ($Stress) { & "$PSScriptRoot/sculpt_stress.ps1" -Duration 600 -RenderedHost }
Write-Host "V04_VERIFY_OK $outDir"
