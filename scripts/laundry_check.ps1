param([switch]$Rendered)
. "$PSScriptRoot/env.ps1"
if ($Rendered) { & (Find-Godot) --path $ProjectRoot -- --laundry-check }
else { & (Find-Godot) --headless --path $ProjectRoot -- --laundry-check }
if ($LASTEXITCODE -ne 0) { throw 'Laundry scene checks failed' }
