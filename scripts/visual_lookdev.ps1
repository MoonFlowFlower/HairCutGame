param([int]$Phase=0,[ValidateSet('Current','Human','Animal','Alien')][string]$Profile='Current',[string]$State='normal',[int]$Players=4,[double]$Seconds=8,[string]$Output='',[switch]$Interactive,[switch]$Impulse,[switch]$Llama,[switch]$FloorCamera,[string[]]$Disable=@(),[ValidateSet('natural','black','blond','red','dyed')][string]$Color='natural')
. "$PSScriptRoot/env.ps1"
if(!$Output){$Output=Join-Path $ProjectRoot ('artifacts/lookdev-p'+$Phase+'-'+$Profile+'-'+$State+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
$launch=@('--path',$ProjectRoot,'--disable-vsync','--','--visual-lookdev','--visual-phase',$Phase,'--hair-profile',$Profile,'--visual-state',$State,'--visual-players',$Players,'--visual-seconds',$Seconds,'--visual-output',$Output,'--visual-fixture',("$ProjectRoot/artifacts/lookdev-fixture-$Players.bin"),'--language','zh')
if($Interactive){$launch+='--visual-interactive'}
if($Impulse){$launch+='--visual-impulse'}
if($Llama){$launch+='--visual-llama'}
if($FloorCamera){$launch+='--visual-floor-camera'}
$launch+=@('--hair-color',$Color)
foreach($flag in $Disable){$launch+='--no-'+$flag}
& (Find-Godot) @launch
if($LASTEXITCODE -ne 0){throw "LookDev failed: $Output"}
Write-Host "LOOKDEV_EVIDENCE $Output"
