param([switch]$Network,[switch]$Rendered,[switch]$SoloCalibration,[switch]$ArchivedA)
. "$PSScriptRoot/env.ps1"
$outDir=Join-Path $ProjectRoot ('artifacts/verify-v06-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
& "$PSScriptRoot/verify_v05.ps1" *> "$outDir/regression.log"
if($LASTEXITCODE -ne 0){throw 'Existing regressions failed'}
& "$PSScriptRoot/v06_check.ps1" *> "$outDir/engine.log"
if($LASTEXITCODE -ne 0){throw 'v0.6 engine checks failed'}
if($Network){
    if($ArchivedA){
    & "$PSScriptRoot/smoke.ps1" -Players 1 -Variant A -Port 17931 *> "$outDir/a-solo.log"
    & "$PSScriptRoot/smoke.ps1" -Players 4 -Variant A -Port 17934 *> "$outDir/a-four.log"
    }
    & "$PSScriptRoot/smoke.ps1" -Players 1 -Variant B -Port 17941 *> "$outDir/b-solo.log"
    & "$PSScriptRoot/smoke.ps1" -Players 2 -Variant B -FullDuration -Port 17942 *> "$outDir/b-two-full.log"
    & "$PSScriptRoot/smoke.ps1" -Players 4 -Variant B -Port 17944 *> "$outDir/b-four.log"
    & "$PSScriptRoot/smoke.ps1" -Players 4 -Variant B -DisconnectClient -Port 17945 *> "$outDir/b-disconnect.log"
    & "$PSScriptRoot/host_loss.ps1" -Variant B *> "$outDir/b-host-loss.log"
}
if($Rendered){& "$PSScriptRoot/v06_review.ps1" *> "$outDir/rendered.log"}
if($SoloCalibration){& "$PSScriptRoot/v06_solo_calibration.ps1" *> "$outDir/solo-calibration.log"}
Write-Host "V06_VERIFY_OK $outDir"
