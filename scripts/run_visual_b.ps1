param([ValidateSet('Solo','Host','Join')][string]$Mode='Solo',[string]$Address='127.0.0.1',[int]$Port=7777,[ValidateSet('Human','Animal','Alien','Current')][string]$Profile='Human',[int]$Phase=7,[string[]]$Disable=@())
. "$PSScriptRoot/env.ps1"
$launch=@('--path',$ProjectRoot,'--','--v06-variant-b','--visual-phase',$Phase,'--hair-profile',$Profile)
if($Mode -eq 'Join'){$launch+=@('--join',$Address,'--port',$Port)}elseif($Mode -eq 'Host'){$launch+=@('--host','--port',$Port)}else{$launch+='--solo'}
foreach($flag in $Disable){$launch+='--no-'+$flag}
& (Find-Godot) @launch
if($LASTEXITCODE -ne 0){throw 'Visual B candidate launch failed'}
