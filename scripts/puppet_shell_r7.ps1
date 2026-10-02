param([ValidateSet('R6_FIBERS','R7_SHELL')][string]$Mode='R7_SHELL',[ValidateSet(4,8,12,16)][int]$Shells=4,[switch]$Interactive,[switch]$Check,[switch]$EdgeOff,[switch]$Motion,[switch]$Flat,[string]$Shot='',[string]$Output='')
$flags=@('r7-review')
if($Mode -eq 'R7_SHELL'){$flags+='r7-shell'}
if($Flat){$flags+='r7-flat'}
if($EdgeOff){$flags+='no-hair-fuzz'}
if($Motion){$flags+='hair-motion'}
$layers=if($Mode -eq 'R7_SHELL'){$Shells}else{0}
if(!$Output){$Output=Join-Path (Split-Path $PSScriptRoot -Parent) ('artifacts/puppet-shell-r7-v04/run-'+$Mode+'-'+$layers+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
& "$PSScriptRoot/puppet_lab.ps1" -Round 6 -HairOptimizeRound 3 -HairShells $layers -HairOptions $flags -Interactive:$Interactive -Check:$Check -Shot $Shot -Output $Output
