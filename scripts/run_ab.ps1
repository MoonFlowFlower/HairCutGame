param([ValidateSet('A','B')][string]$Variant='B',[ValidateSet('Solo','Host','Join')][string]$Mode='Solo',[string]$Address='127.0.0.1',[int]$Port=7777,[string]$Log='',[switch]$LegacyArt)
. "$PSScriptRoot/env.ps1"
if($Variant -eq 'A'){Write-Warning 'Variant A is archived; see docs/archive/variant-a/README.md.'}
$launch=@('--path',$ProjectRoot,'--',('--v06-variant-'+$Variant.ToLowerInvariant()))
if($LegacyArt){$launch=@('--rendering-method','gl_compatibility')+$launch+@('--legacy-b-art')}
if($Mode -eq 'Join'){$launch+=@('--join',$Address,'--port',$Port)}elseif($Mode -eq 'Host'){$launch+=@('--host','--port',$Port)}else{$launch+='--solo'}
if($Log){$launch+=@('--experiment-log',$Log)}
& (Find-Godot) @launch
if($LASTEXITCODE -ne 0){throw 'A/B launch failed'}
