$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $ProjectRoot
$env:MSBuildEnableWorkloadResolver = 'false'
$artifactDir = Join-Path $ProjectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $artifactDir '.gdignore'))) { New-Item -ItemType File -Path (Join-Path $artifactDir '.gdignore') | Out-Null }
function Find-Godot {
    if ($env:GODOT_BIN -and (Test-Path -LiteralPath $env:GODOT_BIN)) { return $env:GODOT_BIN }
    $known = 'D:\Project\Game\GodotEngine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
    if (Test-Path -LiteralPath $known) { return $known }
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    throw 'Godot 4.7.2 Mono not found. Set GODOT_BIN to the console executable.'
}
