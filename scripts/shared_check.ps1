. "$PSScriptRoot/env.ps1"
& (Find-Godot) --headless --path $ProjectRoot -- --solo --shared-check
if ($LASTEXITCODE -ne 0) { throw 'Shared-head physics checks failed' }
