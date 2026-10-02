param([ValidateRange(1,6)][int]$Round=6,[switch]$Interactive,[switch]$Check,[switch]$Probe,[switch]$Reel,[string]$Output='', [ValidateSet('auto','gl_compatibility','forward_plus')][string]$Renderer='auto',[ValidateSet('auto','r5','direct','gi','fabric')][string]$Lighting='auto',[string]$Shot='',[switch]$NoAo,[switch]$NoDirectShadows,[ValidateSet('none','area','omni')][string]$DisableShadowType='none',[ValidateSet('area','spot')][string]$LightRig='spot',[ValidateRange(0,3)][int]$HairOptimizeRound=0,[switch]$HairReview,[int]$HairShells=0,[string[]]$HairOptions=@())
. "$PSScriptRoot/env.ps1"
if($Renderer -eq 'auto') { $Renderer=if($Round -ge 3){'forward_plus'}else{'gl_compatibility'} }
if($Lighting -eq 'auto') { $Lighting=if($Round -ge 6){'fabric'}else{'r5'} }
if (!$Output) { $Output=Join-Path $ProjectRoot ('artifacts/b2-puppet/round'+$Round+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID) }
$Output=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
# Keep the player-facing launcher independent of developer tools such as ripgrep.
$sourceFiles=Get-ChildItem -LiteralPath @('src/Bootstrap','src/Presentation','shaders','scenes','scripts') -Recurse -File |
    Where-Object { $_.Name -like '*puppet*' -and $_.Extension -notin @('.uid','.pyc') }
$textureFiles=Get-ChildItem -LiteralPath 'assets/b2_puppet_textures' -Recurse -File
$files=@(@($sourceFiles)+@($textureFiles) | ForEach-Object { $_.FullName.Substring($ProjectRoot.Length+1) })+@('project.godot','.godot/mono/temp/bin/Debug/ProjectHairball.dll','src/Core/HairVolume.cs')
$files=@($files | Sort-Object -Unique)
# Use the built-in .NET implementation, independent of PS module autoloading.
$hasher=[Security.Cryptography.SHA256]::Create()
try {
    $manifest=foreach($file in $files) {
        $stream=[IO.File]::OpenRead((Join-Path $ProjectRoot $file))
        try { [pscustomobject]@{Path=$file;Sha256=[BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','')} }
        finally { $stream.Dispose() }
    }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'candidate.json')
} finally { $hasher.Dispose() }
$source=Join-Path $Output 'source';New-Item -ItemType Directory -Force -Path $source | Out-Null
foreach($file in $files) { $dest=Join-Path $source $file;New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null;Copy-Item -LiteralPath $file -Destination $dest }
$launch=@('--path',$ProjectRoot,'--rendering-method',$Renderer,'--disable-vsync','--audio-driver','Dummy','res://scenes/PuppetLab.tscn','--','--puppet-round',"$Round",'--puppet-output',$Output)
$launch+=@('--lighting-profile',$Lighting)
if($Shot){$launch+=@('--shot',$Shot)}
if($NoAo){$launch+='--no-ao'}
if($NoDirectShadows){$launch+='--no-direct-shadows'}
$launch+=@('--disable-shadow-type',$DisableShadowType)
$launch+=@('--light-rig',$LightRig)
$launch+=@('--hair-opt-round',"$HairOptimizeRound",'--hair-shells',"$HairShells")
if($HairReview){$launch+='--hair-review'}
foreach($option in $HairOptions){$launch+=('--'+$option)}
if($Interactive){$launch+='--interactive'}
if($Check){$launch+='--puppet-check'}
if($Probe){$launch+='--probe'}
if($Reel){$launch+='--reel'}
& (Find-Godot) @launch 2>&1 | Tee-Object -FilePath (Join-Path $Output 'engine.log')
if($LASTEXITCODE -ne 0){throw "B2 Puppet exited with $LASTEXITCODE : $Output"}
Write-Host "B2_EVIDENCE $Output"
