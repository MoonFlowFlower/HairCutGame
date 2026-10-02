param([switch]$Rendered,[switch]$OrderView)
. "$PSScriptRoot/env.ps1"
$launch=@('--path',$ProjectRoot,'--','--solo','--ego-check','--language','zh')
if ($OrderView) { $Rendered=$true; $launch+='--ego-order-view' }
$name=if ($OrderView) { 'ego-orders.png' } else { 'ego-spotlight.png' }
if ($Rendered) { $launch+=@('--capture',(Join-Path $ProjectRoot ('artifacts/'+$name))) } else { $launch=@('--headless')+$launch }
& (Find-Godot) @launch
if ($LASTEXITCODE -ne 0) { throw 'Co-op With Ego engine checks failed' }
