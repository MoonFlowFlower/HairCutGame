param([switch]$Interactive,[switch]$Color,[switch]$Animal,[string]$Output='')
. "$PSScriptRoot/env.ps1"
if(!$Output){$Output=Join-Path $ProjectRoot ('artifacts/visual-target-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$evidenceFiles=@('src/Bootstrap/VisualTargetLab.cs','src/Presentation/TargetLabGeometry.cs','src/Presentation/TargetLabActors.cs','src/Presentation/TargetLabAssets.cs','scripts/prepare_target_assets.py','src/Bootstrap/Main.cs','project.godot','export_presets.cfg','.godot/mono/temp/bin/Debug/ProjectHairball.dll','assets/third_party/visual_target/ATTRIBUTION.txt')
$evidenceFiles+=Get-ChildItem (Join-Path $ProjectRoot 'assets/third_party/visual_target') -Filter '*.glb' | ForEach-Object { [IO.Path]::GetRelativePath($ProjectRoot,$_.FullName) }
$evidenceFiles | ForEach-Object { [pscustomobject]@{Path=$_;Sha256=(Get-FileHash -LiteralPath (Join-Path $ProjectRoot $_)).Hash} } | ConvertTo-Json | Set-Content (Join-Path $Output 'candidate.json')
$launch=@('--path',$ProjectRoot,'--disable-vsync','--','--visual-target-lab','--target-output',$Output,'--language','zh')
if($Interactive){$launch+='--target-interactive'}
if($Color){$launch+='--target-color'}
if($Animal){$launch+='--target-animal'}
& (Find-Godot) @launch
if($LASTEXITCODE -ne 0){throw "VisualTargetLab failed: $Output"}
Write-Host "VISUAL_TARGET_EVIDENCE $Output"
