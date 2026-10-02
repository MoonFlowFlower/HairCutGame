param([switch]$Rendered)
. "$PSScriptRoot/env.ps1"
if ($Rendered) { & (Find-Godot) --path $ProjectRoot -- --access-check }
else { & (Find-Godot) --headless --path $ProjectRoot -- --access-check }
if ($LASTEXITCODE -ne 0) { throw 'Jump / ladder / view-dependent target checks failed' }
