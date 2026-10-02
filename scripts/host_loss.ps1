param([ValidateSet('Off','A','B')][string]$Variant='Off')
. "$PSScriptRoot/env.ps1"
$engine = Find-Godot
python "$PSScriptRoot/recovery_scenario.py" --executable $engine --profile lan --fault none --host-exit 6 --expect-failure --seconds 45 --port 17782 --variant $Variant
if($LASTEXITCODE -ne 0){throw 'Host loss recovery screen test failed'}
Write-Host 'HOST_LOSS_RECOVERY_SCREEN_OK'
