. "$PSScriptRoot/env.ps1"
& (Find-Godot) --headless --path $ProjectRoot -- --integration
if ($LASTEXITCODE -ne 0) { throw 'Engine integration failed' }
