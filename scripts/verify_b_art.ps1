param([string]$Output='', [switch]$Pilot, [switch]$LegacyArt, [switch]$Bake)
. "$PSScriptRoot/env.ps1"
if(!$Output){$Output=Join-Path $ProjectRoot ('artifacts/b-puppet-mainline/review-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
$Output=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$launch=@('--path',$ProjectRoot,'--audio-driver','Dummy','--disable-vsync','--','--v06-variant-b','--solo','--b-art-check','--qa-output',$Output)
if($Pilot){$launch+='--b-art-pilot'}
if($LegacyArt){$launch+='--legacy-b-art'}
if($Bake){$launch+='--b-art-bake'}
$assembly=Join-Path $ProjectRoot '.godot/mono/temp/bin/Debug/ProjectHairball.dll'
@{assembly=(Get-FileHash -LiteralPath $assembly).Hash;legacy=[bool]$LegacyArt;pilot=[bool]$Pilot;utc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $Output 'candidate.json')
Get-CimInstance Win32_Process | Where-Object {$_.Name -match 'Godot|Hearthstone|python|ffmpeg'} | Select-Object Name,ProcessId | ConvertTo-Json | Set-Content (Join-Path $Output 'processes-before.json')
$quoted=$launch | ForEach-Object {'"'+($_ -replace '"','\"')+'"'}
$p=Start-Process -FilePath (Find-Godot) -WorkingDirectory $ProjectRoot -ArgumentList $quoted -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $Output 'engine.log') -RedirectStandardError (Join-Path $Output 'engine.err')
try{
    if(!$p.WaitForExit(240000)){throw 'B art checks timed out'}
    if($p.ExitCode -ne 0){throw "B art checks exited $($p.ExitCode): $Output"}
    $report=Get-Content (Join-Path $Output 'checks.json') -Raw | ConvertFrom-Json
    if(!$report.pass){throw 'B art checks did not pass'}
    if((Get-Content (Join-Path $Output 'engine.err') -Raw) -match 'ERROR:|Exception'){throw 'Godot error; inspect engine.err'}
    if((Get-FileHash -LiteralPath $assembly).Hash -ne (Get-Content (Join-Path $Output 'candidate.json') -Raw | ConvertFrom-Json).assembly){throw 'Assembly changed during verification'}
    Write-Host "B_ART_VERIFIED $Output"
}finally{if(!$p.HasExited){& taskkill /PID $p.Id /T /F | Out-Null}}
