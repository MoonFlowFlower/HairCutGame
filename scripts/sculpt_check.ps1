param([switch]$Rendered)
. "$PSScriptRoot/env.ps1"
if ($Rendered) { & (Find-Godot) --path $ProjectRoot -- --sculpt-check }
else { & (Find-Godot) --headless --path $ProjectRoot -- --sculpt-check }
if ($LASTEXITCODE -ne 0) { throw 'Sculpt / persistent debris engine checks failed' }
